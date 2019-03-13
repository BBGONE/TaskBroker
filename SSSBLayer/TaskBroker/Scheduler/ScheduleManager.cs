using Coordinator.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TaskBroker.SSSB.EF;

namespace TaskBroker.SSSB.Scheduler
{
    public class ScheduleManager : IScheduleManager
    {
        private readonly ILogger<ScheduleManager> _logger;
        private readonly IServiceProvider _rootServices;
        private readonly IConnectionManager _connectionManager;

        public IServiceProvider RootServices => _rootServices;

        public ScheduleManager(ILogger<ScheduleManager> logger, IConnectionManager connectionManager, IServiceProvider rootProvider) 
        {
            this._logger = logger;
            this._connectionManager = connectionManager;
            this._rootServices = rootProvider;
        }

        /// <summary>
        /// загрузка расписаний
        /// </summary>
        public async Task LoadSchedules()
        {
            using (var scope = _rootServices.CreateScope())
            {
                var provider = scope.ServiceProvider;
                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled))
                {
                    var database = provider.GetRequiredService<SSSBDbContext>();
                    // all tasks which have schedules
                    var tasks = await (from task in database.OnDemandTask.Include(t => t.Shedule)
                                 where task.Active == true && task.SheduleId != null
                                 select task).ToListAsync();

                    lock (BaseSheduleTimer.Timers)
                    {
                        foreach (var task in tasks)
                        {
                            if (task.Shedule.Active == true)
                            {
                                BaseSheduleTimer timer = new ScheduleTimer(this, task.OnDemandTaskId, task.Shedule.Interval);
                                timer.Start();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// загрузка расписания
        /// </summary>
        public async Task ReloadSchedule(int taskID)
        {
            using (var scope = _rootServices.CreateScope())
            {
                var provider = scope.ServiceProvider;
                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled))
                {
                    var database = provider.GetRequiredService<SSSBDbContext>();

                    var task = await database.OnDemandTask.Include(t => t.Shedule).Where(t => t.OnDemandTaskId == taskID).SingleOrDefaultAsync();

                    if (task == null || !(task.Active == true) || task.Shedule == null)
                    {
                        UnLoadSchedule(taskID);
                        return;
                    }

                    lock (BaseSheduleTimer.Timers)
                    {
                        UnLoadSchedule(taskID);

                        if (task.Shedule.Active == true)
                        {
                            BaseSheduleTimer timer = new ScheduleTimer(this, task.OnDemandTaskId, task.Shedule.Interval);
                            timer.Start();
                        }
                    }
                }
            }
        }

        public void UnLoadSchedule(int taskID)
        {
            lock (BaseSheduleTimer.Timers)
            {
                if (BaseSheduleTimer.Timers.ContainsKey(taskID))
                {
                    BaseSheduleTimer timer = BaseSheduleTimer.Timers[taskID];
                    timer.Dispose();
                    BaseSheduleTimer.Timers.Remove(taskID);
                }
            }
        }

        public void UnLoadSchedules()
        {
            lock (BaseSheduleTimer.Timers)
            {
                var timers = from timer in BaseSheduleTimer.Timers
                             select timer.Value;

                var timerslist = timers.ToList();
                while (timerslist.Count() > 0)
                {
                    timerslist[0].Dispose();
                    timerslist.RemoveAt(0);
                }

                BaseSheduleTimer.Timers.Clear();
            }
        }

        public async Task<Guid> SendTimerEvent(int task_id)
        {
            Guid? convesationGroup = Guid.NewGuid();
            using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = await _connectionManager.CreateSSSBConnectionAsync(CancellationToken.None))
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "[PPS].[sp_SendSheduleEvent]";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@task_id", SqlDbType.Int) { Value = task_id });
                cmd.Parameters.Add(new SqlParameter("@relatedConversationGroup", SqlDbType.UniqueIdentifier) { Value = convesationGroup.Value });
                cmd.Parameters.Add(new SqlParameter("@endDialog", SqlDbType.Bit) { Value = false });

                await cmd.ExecuteNonQueryAsync();

                transactionScope.Complete();
            }

            return convesationGroup.Value;
        }
    }
}
