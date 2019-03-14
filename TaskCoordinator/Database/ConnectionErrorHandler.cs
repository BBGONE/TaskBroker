using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Coordinator.Database
{
    public class ConnectionErrorHandler : IConnectionErrorHandler
    {
        private DateTime? _lastTime;
        private int _counter;
        private readonly ILogger<ConnectionErrorHandler> _logger;

        public ConnectionErrorHandler(ILogger<ConnectionErrorHandler> logger) {
            this._counter = 0;
            this._logger = logger;
        }

        private const int MAX_COUNT = 30;
        private const int MAX_AGE_SEC = 3 * 60;

        public async Task Handle(Exception ex, CancellationToken cancelation)
        {
            lock(this)
            {
                DateTime now = DateTime.Now;
                TimeSpan age = TimeSpan.FromDays(1);
                if (_lastTime.HasValue)
                {
                    age = now - _lastTime.Value;
                }
               
                if (_counter < 5)
                {
                    _logger.LogError(new Exception("Can not establish a connection to the Database", ex), "DBConnection error");
                }
                else if (_counter % MAX_COUNT == 0)
                {
                    _logger.LogCritical(new Exception(string.Format("No connection to the Database after {0} attempts", this._counter), ex), "DBConnection error");
                }
                if (age.TotalSeconds > MAX_AGE_SEC)
                    this._counter = 0;
                else
                    this._counter += 1;
                this._lastTime = now;
            }

            int delay = (_counter % MAX_COUNT) * 1000;
            await Task.Delay(delay, cancelation);
        }
    
    }
}
