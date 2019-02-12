using Coordinator.Database;
using Coordinator.SSSB.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace TaskBroker.SSSB.Services
{
    public class PubSubSSSBService : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly IPubSubHelper _pubSubHelper;
        private readonly SSSBService _sssbService;
        private DateTime _startDateTime;
        public const string ONDEMAND_TASK_MESSAGE_TYPE = "PPS_OnDemandTaskMessageType";
        private bool _IsStopNeeded = false;
        private CancellationTokenSource _stopSource;
        private readonly Guid _conversationGroup;
        private readonly HeartBeatTimer _heartBeatTimer;
        private readonly string[] _topics = new string[] { "ADMIN" };

        public PubSubSSSBService(Guid conversationGroup, IServiceProvider services, IPubSubHelper pubSubHelper, HeartBeatTimer heartBeatTimer, ILogger<PubSubSSSBService> logger)
        {
            try
            {
                this._conversationGroup = conversationGroup;
                this._services = services;
                this._pubSubHelper = pubSubHelper;
                this._heartBeatTimer = heartBeatTimer;
                this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
                this._startDateTime = DateTime.Now;

                _sssbService = SSSBService.Create(this._services, (options)=> { options.ConversationGroup = conversationGroup; options.MaxReadersCount = 1; options.Name = PubSubHelper.SUBSCRIBER_SERVICE_NAME; } );
                _sssbService.OnStartedEvent += async () =>
                {
                    await this.OnStarted(_sssbService.QueueName);
                };
                _sssbService.OnStoppedEvent += async () =>
                {
                    await this.OnStopped(_sssbService.QueueName);
                };
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ErrorHelper.GetFullMessage(ex));
                throw;
            }
        }

        protected virtual async Task OnStarted(string QueueName)
        {
            try
            {
                var connectionManager = _services.GetRequiredService<IConnectionManager>();

                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                using (var dbconnection = await connectionManager.CreateSSSBConnectionAsync(_stopSource.Token))
                {
                    foreach (string topic in _topics)
                    {
                        await _pubSubHelper.Subscribe(dbconnection, TimeSpan.FromDays(365 * 10), _conversationGroup, topic);
                    }
                    transactionScope.Complete();
                }

                this._heartBeatTimer.Start();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "PubSubService start error");
            }
        }

        protected virtual async Task OnStopped(string QueueName)
        {
            _heartBeatTimer.Stop();

            var connectionManager = _services.GetRequiredService<IConnectionManager>();
            try
            {
                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                using (var dbconnection = await connectionManager.CreateSSSBConnectionAsync(CancellationToken.None))
                {
                    await _pubSubHelper.UnSubscribe(dbconnection, TimeSpan.FromDays(365 * 10), _conversationGroup);
                    transactionScope.Complete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
        }

        public async Task Start()
        {
            try
            {
                this._stopSource = new CancellationTokenSource();
                _sssbService.RegisterMessageHandler(ONDEMAND_TASK_MESSAGE_TYPE, new TaskMessageHandler(this._services));
                var tasks = _sssbService.Start(_stopSource.Token);
                _IsStopNeeded = true;
                await tasks;
            }
            catch (OperationCanceledException)
            {
                _IsStopNeeded = false;
            }
            catch (PPSException)
            {
                _IsStopNeeded = false;
                throw;
            }
            catch (Exception ex)
            {
                _IsStopNeeded = false;
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ex);
            }
        }

        public async Task Stop()
        {
            try
            {
                await ServiceStop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw;
            }
        }

        public void Pause()
        {
            this._sssbService.Pause();
        }

        public void Resume()
        {
            this._sssbService.Resume();
        }

        /// <summary>
        /// returns local datetime when service started working
        /// after recycling it is initiated once again
        /// </summary>
        public DateTime StartDateTime
        {
            get
            {
                return this._startDateTime;
            }
        }

        public string Name
        {
            get { return nameof(PubSubSSSBService); }
        }

        public void ActivateQueue(string name)
        {
            this._sssbService.QueueActivator.ActivateQueue();
        }

        protected virtual async Task ServiceStop()
        {
            if (!_IsStopNeeded)
                return;
            try
            {
                _IsStopNeeded = false;
                await Task.WhenAny(_sssbService.Stop(), Task.Delay(TimeSpan.FromSeconds(30)));
            }
            catch (OperationCanceledException)
            {
                // NOOP
            }
            catch (PPSException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ex);
            }
            finally
            {
                _sssbService.UnregisterMessageHandler(ONDEMAND_TASK_MESSAGE_TYPE);
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            try
            {
                if (_IsStopNeeded)
                {
                    this.Stop().Wait(TimeSpan.FromSeconds(30));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
        }

        #endregion
    }
}
