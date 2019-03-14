using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.MessageHandlers;

namespace SSSBLayer.TaskBroker.Factories
{
    public interface ICustomMessageHandlerFactory
    {
        ICustomMessageHandler Create(ServiceMessageEventArgs args);
    }
}