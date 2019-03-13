using Coordinator.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Transactions;

namespace TaskBroker.SSSB.Utils
{
    public class HeartBeatTimer: IDisposable
    {
        private System.Threading.Timer _timer;
        private const int INTERVAL = 300; // 5 minutes
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly IPubSubHelper _pubSubHelper;
        private readonly Guid _conversationGroup;
        private bool _isStopped;

        public HeartBeatTimer(Guid conversationGroup, IServiceProvider services, IPubSubHelper pubSubHelper, ILogger<HeartBeatTimer> logger)
        {
            this._conversationGroup = conversationGroup;
            this._services = services;
            this._pubSubHelper = pubSubHelper;
            this._logger = logger;
            this._isStopped = true;

            this._timer = new System.Threading.Timer(async (state) => {
                try
                {
                    this.Pause();
                    var connectionManager = _services.GetRequiredService<IConnectionManager>();

                    using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                    using (var dbconnection = await connectionManager.CreateSSSBConnectionAsync(CancellationToken.None))
                    {
                        await _pubSubHelper.HeartBeat(dbconnection, TimeSpan.FromDays(365 * 10), _conversationGroup);
                        transactionScope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                finally
                {
                    this.Resume();
                }
            });
        }

        public void Start()
        {
            if (!disposedValue && _isStopped)
            {
                _isStopped = false;
                _timer.Change((int)TimeSpan.FromSeconds(5).TotalMilliseconds, (int)TimeSpan.FromSeconds(INTERVAL).TotalMilliseconds);
            }
        }

        public void Stop()
        {
            if (!disposedValue && !_isStopped)
            {
                _isStopped = true;
                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }


        private void Pause()
        {
            if (!disposedValue && !_isStopped)
            {
                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }

        }

        private void Resume()
        {
            if (!disposedValue && !_isStopped)
            {
                _timer.Change((int)TimeSpan.FromSeconds(INTERVAL).TotalMilliseconds, (int)TimeSpan.FromSeconds(INTERVAL).TotalMilliseconds);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._timer.Dispose();
                }

                this._isStopped = true;
                this._timer = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
