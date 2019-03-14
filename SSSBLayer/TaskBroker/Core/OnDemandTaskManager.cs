using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Transactions;
using TaskBroker.SSSB.EF;
using TaskBroker.SSSB.Executors;

namespace TaskBroker.SSSB.Core
{
    public class OnDemandTaskManager : BaseManager, IOnDemandTaskManager
    {
        private Guid _id = Guid.Empty;
        private static readonly ConcurrentDictionary<int, TaskInfo> _taskInfos = new ConcurrentDictionary<int, TaskInfo>();

        public OnDemandTaskManager(IServiceProvider services, ISettingsService settings) :
            base(services)
        {
            this.Settings = settings;
        }

        public Guid ID
        {
            get
            {
                if (_id == Guid.Empty)
                {
                    _id = Guid.NewGuid();
                }
                return this._id;
            }
        }
      
        public IExecutor CurrentExecutor
        {
            get;
            set;
        }

        public ISettingsService Settings
        {
            get;
        }

        public async Task<TaskInfo> GetTaskInfo(int id)
        {
            if (_taskInfos.TryGetValue(id, out TaskInfo res))
                return res;

            using (var scope = this.Services.CreateScope())
            {
                var provider = scope.ServiceProvider;
                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled))
                {
                    var database = provider.GetRequiredService<SSSBDbContext>();
                    OnDemandTask task = await database.OnDemandTask.Include((d)=> d.Executor).SingleOrDefaultAsync(t => t.OnDemandTaskId == id);
                    if (task == null)
                        throw new Exception(string.Format("OnDemandTask with taskID={0} was not found", id));
                    res = TaskInfo.FromOnDemandTask(task);
                }
            }

            _taskInfos.TryAdd(id, res);
            return res;
        }

        public static void FlushTaskInfos()
        {
            _taskInfos.Clear();
        }

        protected override void OnDispose()
        {
            try
            {
                var executor = this.CurrentExecutor;
                if (executor != null)
                {
                    this.CurrentExecutor = null;
                    (executor as IDisposable)?.Dispose();
                }
            }
            finally
            {
                base.OnDispose();
            }
        }
    }
}