using Coordinator;
using Coordinator.Database;
using Microsoft.Extensions.Logging;
using System;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.Factories
{
    public class SSSBMessageReaderFactory : IMessageReaderFactory
    {
        private readonly ISSSBService _service;
        private readonly ILogger _log;
        private readonly ISSSBMessageDispatcher _messageDispatcher;
        private readonly IConnectionErrorHandler _errorHandler;
        private readonly ISSSBManager _manager;
        private readonly IConnectionManager _connectionManager;
        private readonly Guid? _conversation_group;

        public SSSBMessageReaderFactory(ISSSBService service, ISSSBMessageDispatcher messageDispatcher, ILoggerFactory loggerFactory, 
            IConnectionErrorHandler errorHandler, ISSSBManager manager, IConnectionManager connectionManager)
        {
            this._log = loggerFactory.CreateLogger(nameof(SSSBMessageReader));
            this._service = service;
            this._conversation_group = service.ConversationGroup;
            this._messageDispatcher = messageDispatcher;
            this._errorHandler = errorHandler;
            this._manager = manager;
            this._connectionManager = connectionManager;
        }

        public IMessageReader CreateReader(long taskId, BaseTasksCoordinator coordinator)
        {
            return new SSSBMessageReader(taskId, _conversation_group, coordinator, _log, _service, _messageDispatcher,
                _errorHandler, _manager, _connectionManager);
        }
    }
}
