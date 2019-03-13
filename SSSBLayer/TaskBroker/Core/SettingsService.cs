using Coordinator.Database;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace TaskBroker.SSSB.Core
{
    public class SettingsService : ISettingsService
    {
        private static readonly ConcurrentDictionary<int, Task<ExecutorSettings>> _staticSettings = new ConcurrentDictionary<int, Task<ExecutorSettings>>();
        private readonly IServiceProvider _services;

        public SettingsService(IServiceProvider services)
        {
            this._services = services;
        }

        private async Task<string> GetExecutorSettings(int taskID)
        {
            var connectionManager = this._services.GetRequiredService<IConnectionManager>();
            using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = await connectionManager.CreateSSSBConnectionAsync(CancellationToken.None))
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "[PPS].[sp_GetOnDemandExecutorStaticSettings]";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("taskID", SqlDbType.Int) { Value = taskID });
                cmd.Parameters.Add(new SqlParameter("@executorSettings", SqlDbType.Xml) { Direction = ParameterDirection.Output });

                await cmd.ExecuteNonQueryAsync();

                return cmd.Parameters["@executorSettings"].Value?.ToString();
            }
        }

        /// <summary>
        /// получить настройки экзекутора
        /// </summary>
        private async Task<string> GetSettings(int settingID)
        {
            var connectionManager = this._services.GetRequiredService<IConnectionManager>();
            using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = await connectionManager.CreateSSSBConnectionAsync(CancellationToken.None))
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "[PPS].[sp_GetStaticSettings]";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@settingID", SqlDbType.Int) { Value = settingID });
                cmd.Parameters.Add(new SqlParameter("@ExecutorSettings", SqlDbType.Xml) { Direction = ParameterDirection.Output });

                await cmd.ExecuteNonQueryAsync();

                return cmd.Parameters["@ExecutorSettings"].Value?.ToString();
            }
        }

        public Task<ExecutorSettings> GetByID(int settingID)
        {
            return _staticSettings.GetOrAdd(settingID, (key) => {
                Task<string> settings = GetSettings(key);
                return settings.ContinueWith((antecedent) => new ExecutorSettings(antecedent.Result));
            });
        }

        public Task<ExecutorSettings> GetByTaskID(int taskID)
        {
            Task<string> settings = GetExecutorSettings(taskID);
            return settings.ContinueWith((antecedent) => new ExecutorSettings(antecedent.Result));
        }

        public async Task<T> GetByID<T>(int settingID)
        where T : class
        {
            ExecutorSettings settings = default(ExecutorSettings);
            try
            {
                settings = await GetByID(settingID);
            }
            catch
            {
                _staticSettings.TryRemove(settingID, out var _);
                throw;
            }

            return settings.GetDeserialized<T>();
        }

        public async Task<T> GetByTaskID<T>(int taskID)
            where T : class
        {
            ExecutorSettings settings = default(ExecutorSettings);
            settings = await GetByTaskID(taskID);
            return settings.GetDeserialized<T>();
        }

        public static void FlushStaticSettings()
        {
            _staticSettings.Clear();
        }

        public static void FlushStaticSettings(int settingID)
        {
            _staticSettings.TryRemove(settingID, out var _);
        }
    }
}
