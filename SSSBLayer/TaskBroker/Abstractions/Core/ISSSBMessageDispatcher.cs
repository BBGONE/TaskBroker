using Coordinator;
using System.Data.SqlClient;
using TaskBroker.SSSB.Errors;
using TaskBroker.SSSB.MessageHandlers;

namespace TaskBroker.SSSB.Core
{
    public interface ISSSBMessageDispatcher: IMessageDispatcher<SSSBMessage, SqlConnection>
    {
        void RegisterMessageHandler(string messageType, IMessageHandler<ServiceMessageEventArgs> handler);
        void RegisterErrorMessageHandler(string messageType, IMessageHandler<ErrorMessageEventArgs> handler);
        void UnregisterMessageHandler(string messageType);
        void UnregisterErrorMessageHandler(string messageType);
    }
}
