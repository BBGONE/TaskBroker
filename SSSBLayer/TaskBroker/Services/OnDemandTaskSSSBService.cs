using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.MessageHandlers;

namespace TaskBroker.SSSB.Services
{
    public class OnDemandTaskSSSBService : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly SSSBService _sssbService;
        private DateTime _startDateTime;
        public const string ONDEMAND_TASK_SERVICE_NAME = "PPS_OnDemandTaskService";
        public const string ONDEMAND_TASK_MESSAGE_TYPE = "PPS_OnDemandTaskMessageType";
        public const string DEFERED_MESSAGE_TYPE = "PPS_DeferedMessageType";
        private bool _IsStopNeeded = false;
        private CancellationTokenSource _stopSource;

        public OnDemandTaskSSSBService(IServiceProvider services, ILogger<OnDemandTaskSSSBService> logger)
        {
            try
            {
                this._services = services;
                this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
                this._startDateTime = DateTime.Now;

                _sssbService = SSSBService.Create(this._services, (options)=> { options.MaxReadersCount = 4; options.Name = ONDEMAND_TASK_SERVICE_NAME; } );
                _sssbService.OnStartedEvent += async () =>
                {
                    this._startDateTime = DateTime.Now;
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

        protected virtual Task OnStarted(string QueueName)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnStopped(string QueueName)
        {
            return Task.CompletedTask;
        }

        public async Task Start()
        {
            try
            {
                this._stopSource = new CancellationTokenSource();
                _sssbService.RegisterMessageHandler(ONDEMAND_TASK_MESSAGE_TYPE, new TaskMessageHandler(this._services));
                _sssbService.RegisterMessageHandler(DEFERED_MESSAGE_TYPE, new DeferedMessageHandler(this._services));
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
            get { return nameof(OnDemandTaskSSSBService); }
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
                var task2 = Task.WhenAny(_sssbService.Stop(), Task.Delay(TimeSpan.FromSeconds(30)));
                await task2;
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
