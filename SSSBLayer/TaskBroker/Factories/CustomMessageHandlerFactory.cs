using System;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.MessageHandlers;

namespace SSSBLayer.TaskBroker.Factories
{
    public class CustomMessageHandlerFactory : ICustomMessageHandlerFactory
    {
        private readonly Func<ServiceMessageEventArgs, ICustomMessageHandler> _func;

        public CustomMessageHandlerFactory(Func<ServiceMessageEventArgs, ICustomMessageHandler> func)
        {
            this._func = func;
        }

        public ICustomMessageHandler Create(ServiceMessageEventArgs args)
        {
            return _func(args);
        }
    }
}
