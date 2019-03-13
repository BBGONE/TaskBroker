using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.Utils
{
    public interface IServiceBrokerHelper
    {
        Task<Guid> BeginDialogConversation(SqlConnection dbconnection, string fromService, string toService, string contractName, TimeSpan lifetime, bool withEncryption, Guid? relatedConversationHandle, Guid? relatedConversationGroupID);
        Task EndConversation(SqlConnection dbconnection, Guid conversationHandle);
        Task EndConversationWithCleanup(SqlConnection dbconnection, Guid conversationHandle);
        Task EndConversationWithError(SqlConnection dbconnection, Guid conversationHandle, int errorCode, string errorDescription);
        Task<string> GetServiceQueueName(string serviceName);
        Task SendMessage(SqlConnection dbconnection, SSSBMessage message);
        Task SendMessage(SqlConnection dbconnection, Guid conversationHandle, string messageType, XElement xmlBody = null);
        Task<long?> SendPendingMessage(SqlConnection dbconnection,
            SSSBMessage message,
            string fromService, 
            TimeSpan lifetime, 
            bool isWithEncryption, 
            DateTime activationTime, 
            string objectID,
            int attemptNumber);
        Task<int> ProcessPendingMessages(SqlConnection dbconnection, bool processAll = false, string objectID = null);
    }
}