using System.Data.SqlClient;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.MessageHandlers
{
    public interface ICustomMessageHandler
    {
        Task HandleMessage(SqlConnection dbconnection, IMessageHandler<ServiceMessageEventArgs> messageHandler, ServiceMessageEventArgs serviceArgs);
    }
}