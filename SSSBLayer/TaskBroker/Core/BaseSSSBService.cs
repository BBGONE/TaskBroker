using Coordinator;
using Coordinator.Database;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using Shared.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Errors;
using TaskBroker.SSSB.Factories;
using TaskBroker.SSSB.MessageHandlers;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Core
{
    /// <summary>
    /// A Service to Process Sql Server Service Broker messages
    /// </summary>
    public class BaseSSSBService<TDispatcher, TMessageReaderFactory> : ISSSBService
        where TDispatcher: class, ISSSBMessageDispatcher
        where TMessageReaderFactory: class, IMessageReaderFactory
    {
        private readonly IErrorMessages _errorMessages;

        #region Private Fields
        private volatile bool _isStopped;
        private CancellationTokenSource _stopStartingSource;
        private string _queueName;
        private readonly Lazy<BaseTasksCoordinator> _tasksCoordinator;
        private readonly Lazy<ISSSBMessageDispatcher> _messageDispatcher;
        private readonly IConnectionManager _connectionManager;
        private readonly IServiceBrokerHelper _serviceBrokerHelper;
        private CancellationToken _rootToken;
        #endregion

        public BaseSSSBService(ServiceOptions options, IDependencyResolver<TDispatcher, TMessageReaderFactory> dependencyResolver)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (dependencyResolver == null)
                throw new ArgumentNullException(nameof(dependencyResolver));

            this.Name = options.Name;
            this._queueName = string.Empty;
            this._isStopped = true;
            this._rootToken = CancellationToken.None;

            this.IsQueueActivationEnabled = options.IsQueueActivationEnabled;
            this.MaxReadersCount = options.MaxReadersCount;
            this.MaxReadParallelism = options.MaxReadParallelism;
            this.ConversationGroup = options.ConversationGroup;

            this.Logger = dependencyResolver.LoggerFactory.CreateLogger(this.GetType().Name);
            this._serviceBrokerHelper = dependencyResolver.ServiceBrokerHelper;
            this._connectionManager = dependencyResolver.ConnectionManager;
            this._errorMessages = dependencyResolver.ErrorMessages;
            this._messageDispatcher = dependencyResolver.GetMessageDispatcher(this);
            this._tasksCoordinator = dependencyResolver.GetTaskCoordinator(this);
        }

        public AsyncEventHandler OnStartedEvent;
        public AsyncEventHandler OnStoppedEvent;

        protected virtual async Task InternalStart()
        {
            try
            {
                _queueName = await _serviceBrokerHelper.GetServiceQueueName(this.Name);
                if (_queueName == null)
                    throw new Exception(string.Format(ServiceBrokerResources.ServiceInitializationErrMsg, this.Name));
                this._tasksCoordinator.Value.Start(this._rootToken);
                await this.OnStarted();
            }
            catch (Exception ex)
            {
                throw new Exception(ServiceBrokerResources.StartErrMsg, ex);
            }
        }

        #region OnEvent Methods
        protected virtual Task OnStarting()
        {
            return Task.CompletedTask;
        }

        protected virtual async Task OnStarted()
        {
            var invocationList = this.OnStartedEvent?.GetInvocationList()?.Cast<AsyncEventHandler>() ?? Enumerable.Empty<AsyncEventHandler>();
            foreach(var item in invocationList)
            {
                await item.Invoke();
            }
            
        }

        protected virtual async Task OnStopped()
        {
            var invocationList = this.OnStoppedEvent?.GetInvocationList()?.Cast<AsyncEventHandler>() ?? Enumerable.Empty<AsyncEventHandler>();
            foreach (var item in invocationList)
            {
                await item.Invoke();
            }
        }
        #endregion

        #region Windows Service Public Methods
        /// <summary>
		/// Запуск сервиса.
		/// Запускается QueueReadersCount читателей очереди сообщений с бесконечным циклом обработки.
		/// </summary>
		public async Task Start(CancellationToken token= default(CancellationToken))
        {
            if (!this.IsStopped)
                throw new InvalidOperationException(string.Format("Service: {0} has not finished the execution", this.Name));
            await this.OnStarting();
            this._isStopped = false;
            this._rootToken = token;
            var tokenSource = new CancellationTokenSource();
            this._stopStartingSource = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, _rootToken);
            CancellationToken ct = this._stopStartingSource.Token;
            await Task.Run(async () => {
                var svc = this;
                try
                {
                    int i = 0;
                    while (!ct.IsCancellationRequested && !(await _connectionManager.IsSSSBConnectionOKAsync()))
                    {
                        ++i;
                        if (i >= 3 && i <= 7)
                            Logger.LogError(string.Format("Can not connect to the database in the SSSB service: {0}", this.Name));
                        if ((i % 20) == 0)
                            throw new Exception(string.Format("After 20 attempts, still can not connect to the database in the SSSB service: {0}!", this.Name));

                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }

                    ct.ThrowIfCancellationRequested();
                    this._stopStartingSource = null;
                    await this.InternalStart();
                }
                catch (OperationCanceledException)
                {
                    this._isStopped = true;
                    this._stopStartingSource = null;
                }
                catch (Exception ex)
                {
                    if (!(ex is PPSException))
                    {
                        Logger.LogCritical(ErrorHelper.GetFullMessage(ex));
                    }
                    this._stopStartingSource.Cancel();
                    this._isStopped = true;
                    this._stopStartingSource = null;
                }
            }, ct);
        }

        /// <summary>
        /// Остановка сервиса.
        /// </summary>
        public async Task Stop()
        {
            try
            {
                lock (this)
                {
                    if (this._stopStartingSource != null)
                    {
                        this._stopStartingSource.Cancel();
                        this._stopStartingSource = null;
                        return;
                    }
                    _isStopped = true;
                }
                await this.OnStopped();
                await this._tasksCoordinator.Value.Stop();
            }
            catch (OperationCanceledException)
            {
                //NOOP
            }
            catch (PPSException)
            {
                // Already Logged
            }
            catch (Exception ex)
            {
                Logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
        }

        /// <summary>
        /// приостанавливает обработку сообщений
        /// </summary>
        public void Pause()
        {
            if (this._tasksCoordinator.IsValueCreated)
                this._tasksCoordinator.Value.IsPaused = true;
        }

        /// <summary>
        /// возобновляет обработку сообщений, если была приостановлена
        /// </summary>
        public void Resume()
        {
            if (this._tasksCoordinator.IsValueCreated)
                this._tasksCoordinator.Value.IsPaused = false;
        }
        #endregion

        #region Public Methods
        /// <summary>
		/// Регистрация обработчика сообщений заданного типа.
		/// </summary>
		/// <param name="messageType"></param>
		/// <param name="handler"></param>
		public void RegisterMessageHandler(string messageType, IMessageHandler<ServiceMessageEventArgs> handler)
        {
            this._messageDispatcher.Value.RegisterMessageHandler(messageType, handler);
        }

        /// <summary>
        /// Регистрация обработчика ошибок обработки сообщений заданного типа.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="handler"></param>
        public void RegisterErrorMessageHandler(string messageType, IMessageHandler<ErrorMessageEventArgs> handler)
        {
            this._messageDispatcher.Value.RegisterErrorMessageHandler(messageType, handler);
        }

        /// <summary>
        /// Отмена регистрации обработчика сообщений заданного типа.
        /// </summary>
        /// <param name="messageType"></param>
        public void UnregisterMessageHandler(string messageType)
        {
            this._messageDispatcher.Value.UnregisterMessageHandler(messageType);
        }

        /// <summary>
        /// Отмена регистрации обработчика сообщений заданного типа.
        /// </summary>
        /// <param name="messageType"></param>
        public void UnregisterErrorMessageHandler(string messageType)
        {
            this._messageDispatcher.Value.UnregisterErrorMessageHandler(messageType);
        }
        #endregion

        #region ITaskService Members
        string ITaskService.Name
        {
            get
            {
                return this.Name;
            }
        }

        public IQueueActivator QueueActivator
        {
            get
            {
                return _tasksCoordinator as IQueueActivator;
            }
        }

        public bool IsQueueActivationEnabled { get; private set; }

        ErrorMessage ISSSBService.GetError(Guid messageID)
        {
            return _errorMessages.GetError(messageID);
        }

        int ISSSBService.AddError(Guid messageID, Exception err)
        {
            return _errorMessages.AddError(messageID, err);
        }
        #endregion

        #region Properties
        public bool IsStopped
        {
            get { return _isStopped; }
        }

        public bool IsPaused
        {
            get
            {
                return (IsStopped || !this._tasksCoordinator.IsValueCreated)? false: this._tasksCoordinator.Value.IsPaused;
            }
        }

        public string Name
        {
            get;
        }

        public string QueueName
        {
            get
            {
                return _queueName;
            }
        }

        protected ILogger Logger
        {
            get;
        }

        public int MaxReadersCount
        {
            get;
        }

        public int MaxReadParallelism
        {
            get;
        }

        public Guid? ConversationGroup
        {
            get;
        }
        #endregion
    }
}