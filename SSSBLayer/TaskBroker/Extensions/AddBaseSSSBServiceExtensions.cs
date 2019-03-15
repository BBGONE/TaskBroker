using Coordinator.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SSSBLayer.TaskBroker.Factories;
using TaskBroker.SSSB.Errors;
using TaskBroker.SSSB.Factories;
using TaskBroker.SSSB.MessageHandlers;
using TaskBroker.SSSB.Results;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Core
{
    public static class AddBaseSSSBServiceExtensions
    {
        public static void AddSSSBService(this IServiceCollection services)
        {
            services.TryAddTransient<IConnectionErrorHandler, ConnectionErrorHandler>();
            services.TryAddTransient(typeof(IDependencyResolver<,>), typeof(DependencyResolver<,>));
            services.TryAddSingleton<IErrorMessages, ErrorMessages>();
            services.TryAddSingleton<IConnectionManager, ConnectionManager>();
            services.TryAddSingleton<ISSSBManager, SSSBManager>();
            services.TryAddSingleton<IServiceBrokerHelper, ServiceBrokerHelper>();
            services.TryAddSingleton<IStandardMessageHandlers, StandardMessageHandlers>();
            var handlerFactory = ActivatorUtilities.CreateFactory(typeof(CustomMessageHandler), new System.Type[] { typeof(ISSSBService) });
            services.TryAddSingleton<ICustomMessageHandlerFactory>(new CustomMessageHandlerFactory((args) =>
                (ICustomMessageHandler)handlerFactory(args.Services, new object[] { args.SSSBService })));
            services.TryAddSingleton<NoopMessageResult>();
        }
    }
}
