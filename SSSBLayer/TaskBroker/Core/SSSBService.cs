using Microsoft.Extensions.DependencyInjection;
using System;
using TaskBroker.SSSB.Factories;

namespace TaskBroker.SSSB.Core
{
    public class SSSBService : BaseSSSBService<SSSBMessageDispatcher, SSSBMessageReaderFactory>
    {
        public SSSBService(ServiceOptions options, IDependencyResolver<SSSBMessageDispatcher, SSSBMessageReaderFactory> dependencyResolver) 
            : base(options, dependencyResolver)
        {
            
        }

        public static SSSBService Create(IServiceProvider serviceProvider, Action<ServiceOptions> configure)
        {
            ServiceOptions serviceOptions = new ServiceOptions() { IsQueueActivationEnabled = false, MaxReadersCount = 1, Name = null };
            configure?.Invoke(serviceOptions);
            if (string.IsNullOrEmpty(serviceOptions.Name))
                throw new ArgumentException($"SSSBService must have a Name");
            return ActivatorUtilities.CreateInstance<SSSBService>(serviceProvider, new object[] { serviceOptions });
        }
    }
}
