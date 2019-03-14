using Microsoft.Extensions.Configuration;
using Shared.Database;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Coordinator.Database
{
    public class ConnectionManager : IConnectionManager
    {
        public const string CONNECTION_STRING_NAME = "DBConnectionStringSSSB";

        private readonly Lazy<DbNameConnectionFactory> _dbNameFactory;
        private readonly Lazy<DbConnectionFactory> _dbFactory;
        private readonly IConfiguration _configuration;

        public ConnectionManager(IConfiguration configuration)
        {
            _configuration = configuration;
            _dbNameFactory = new Lazy<DbNameConnectionFactory>(() => new DbNameConnectionFactory(_configuration, CONNECTION_STRING_NAME), true);
            _dbFactory = new Lazy<DbConnectionFactory>(() => new DbConnectionFactory(_configuration), true);
        }

        public async Task<SqlConnection> GetConnectionByDbNameAsync(string dbname)
        {
            return await DbConnectionScope.GetOpenConnectionAsync<SqlConnection>(_dbNameFactory.Value, dbname);
        }

        public async Task<SqlConnection> GetConnectionByNameAsync(string connectionName)
        {
            return await DbConnectionScope.GetOpenConnectionAsync<SqlConnection>(_dbFactory.Value, connectionName);
        }

        public async Task<SqlConnection> CreateConnectionByDbNameAsync(string dbname)
        {
            SqlConnection cn = (SqlConnection)_dbNameFactory.Value.CreateConnection(dbname);
            if (cn.State == System.Data.ConnectionState.Closed)
                await cn.OpenAsync();
            return cn;
        }

        public async Task<SqlConnection> CreateConnectionByNameAsync(string connectionName)
        {
            SqlConnection cn = (SqlConnection)_dbFactory.Value.CreateConnection(connectionName);
            if (cn.State == System.Data.ConnectionState.Closed)
                await cn.OpenAsync();
            return cn;
        }

        public string GetSSSBConnectionString()
        {
            string connstring = _configuration.GetConnectionString(CONNECTION_STRING_NAME);
            if (string.IsNullOrEmpty(connstring))
            {
                throw new Exception(string.Format("Не найдена строка соединения {0} в файле конфигурации", CONNECTION_STRING_NAME));
            }
            return connstring;
        }

        public async Task<bool> CheckSSSBConnectionAsync()
        {
            try
            {
                using (var conn = await CreateConnectionByNameAsync(CONNECTION_STRING_NAME))
                {
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<SqlConnection> GetSSSBConnectionAsync()
        {
            return await GetConnectionByNameAsync(CONNECTION_STRING_NAME);
        }

        public async Task<SqlConnection> CreateSSSBConnectionAsync(CancellationToken token)
        {
            return await CreateConnectionByNameAsync(CONNECTION_STRING_NAME);
        }

        public async Task<bool> IsSSSBConnectionOKAsync()
        {
            try
            {
                using (DbScope dbScope = new DbScope(TransactionScopeOption.Suppress))
                {
                    var cn = await GetSSSBConnectionAsync();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
