using Coordinator;
using Coordinator.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Errors;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Factories
{
    public class DependencyResolver<TDispatcher, TMessageReaderFactory> : IDependencyResolver<TDispatcher, TMessageReaderFactory>
        where TDispatcher : class, ISSSBMessageDispatcher
        where TMessageReaderFactory : class, IMessageReaderFactory
    {
        private readonly IServiceProvider services;
        private Lazy<ISSSBMessageDispatcher> _messageDispatcher;
        private Lazy<IMessageReaderFactory> _messageReaderFactory;
        private Lazy<BaseTasksCoordinator> _tasksCoordinator;

        public DependencyResolver(IServiceProvider services)
        {
            this.services = services;
        }

        public ILoggerFactory LoggerFactory
        {
            get
            {
                return services.GetRequiredService<ILoggerFactory>();
            }
        }

        public IErrorMessages ErrorMessages
        {
            get
            {
                return services.GetRequiredService<IErrorMessages>();
            }
        }

        public IConnectionManager ConnectionManager
        {
            get
            {
                return services.GetRequiredService<IConnectionManager>();
            }
        }

        public IServiceBrokerHelper ServiceBrokerHelper
        {
            get
            {
                return services.GetRequiredService<IServiceBrokerHelper>();
            }
        }

        public Lazy<IMessageReaderFactory> GetMessageReaderFactory(BaseSSSBService<TDispatcher, TMessageReaderFactory> service)
        {
            if (this._messageReaderFactory == null)
            {
                this._messageReaderFactory = new Lazy<IMessageReaderFactory>(() =>
                {
                    return ActivatorUtilities.CreateInstance<TMessageReaderFactory>(services, new object[] { service, GetMessageDispatcher(service).Value });
                }, true);
            }

            return this._messageReaderFactory;
        }

        public Lazy<ISSSBMessageDispatcher> GetMessageDispatcher(BaseSSSBService<TDispatcher, TMessageReaderFactory> service)
        {
            if (this._messageDispatcher == null)
            {
                this._messageDispatcher = new Lazy<ISSSBMessageDispatcher>(() =>
                {
                    return ActivatorUtilities.CreateInstance<TDispatcher>(services, new object[] { service });
                }, true);
            }

            return this._messageDispatcher;
        }

        public Lazy<BaseTasksCoordinator> GetTaskCoordinator(BaseSSSBService<TDispatcher, TMessageReaderFactory> service)
        {
            if (this._tasksCoordinator == null)
            {
                this._tasksCoordinator = new Lazy<BaseTasksCoordinator>(() =>
                {
                    return new SSSBTasksCoordinator(LoggerFactory, GetMessageReaderFactory(service).Value, service.MaxReadersCount, service.IsQueueActivationEnabled, service.MaxReadParallelism);
                }, true);
            }

            return this._tasksCoordinator;
        }
    }
}
