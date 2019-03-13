using Coordinator;
using Coordinator.Database;
using Microsoft.Extensions.Logging;
using System;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Errors;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Factories
{
    public interface IDependencyResolver<TDispatcher, TMessageReaderFactory>
        where TDispatcher : class, ISSSBMessageDispatcher
        where TMessageReaderFactory : class, IMessageReaderFactory
    {
        IConnectionManager ConnectionManager { get; }
        IErrorMessages ErrorMessages { get; }
        ILoggerFactory LoggerFactory { get; }
        IServiceBrokerHelper ServiceBrokerHelper { get; }

        Lazy<ISSSBMessageDispatcher> GetMessageDispatcher(BaseSSSBService<TDispatcher, TMessageReaderFactory> service);
        Lazy<IMessageReaderFactory> GetMessageReaderFactory(BaseSSSBService<TDispatcher, TMessageReaderFactory> service);
        Lazy<BaseTasksCoordinator> GetTaskCoordinator(BaseSSSBService<TDispatcher, TMessageReaderFactory> service);
    }
}