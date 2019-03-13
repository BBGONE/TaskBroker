using System.Data.SqlClient;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.Utils
{
    public interface IStandardMessageHandlers
    {
        Task EchoMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage);
        Task ErrorMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage);
        Task EndDialogMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage);
        Task EndDialogMessageWithErrorHandler(SqlConnection dbconnection, SSSBMessage receivedMessage, string message, int errorNumber);
    }
}