using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace TaskBroker.SSSB.Core
{
    public interface ISSSBManager
    {
        Task<Guid?> BeginDialogConversation(SqlConnection dbconnection, string fromService, string toService, string contractName, int? lifetime, bool? withEncryption, Guid? relatedConversationID, Guid? relatedConversationGroupID);
        Task EnableQueue(string queueName);
        Task<int> EndConversation(SqlConnection dbconnection, Guid? conversationHandle, bool? withCleanup, int? errorCode, string errorDescription);
        Task<string> GetServiceQueueName(string serviceName);
        Task<bool> IsQueueEnabled(string queueName);
        Task<IDataReader> ReceiveMessagesAsync(SqlConnection dbconnection, string queueName, int? fetchSize, int? waitTimeout, Guid? conversation_group, CommandBehavior procedureResultBehaviour, CancellationToken cancellation);
        Task<IDataReader> ReceiveMessagesNoWaitAsync(SqlConnection dbconnection, string queueName, int? fetchSize, Guid? conversation_group, CommandBehavior procedureResultBehaviour, CancellationToken cancellation);
        Task<int> SendMessage(SqlConnection dbconnection, Guid? conversationHandle, string messageType, byte[] body);
        Task<int> SendMessageWithInitiatorConversationGroup(
               SqlConnection dbconnection,
               string fromService,
               string toService,
               string contractName,
               int? lifeTime,
               bool? isWithEncryption,
               Guid initiatorConversationGroupID,
               String messageType,
               byte[] body,
               bool? withEndDialog = null);
        Task<long?> SendPendingMessage(
            SqlConnection dbconnection,
            string objectID, 
            DateTime? activationDate, 
            string fromService, 
            string toService, 
            string contractName, 
            int? lifeTime, 
            bool? isWithEncryption, 
            byte[] messageBody, 
            string messageType);
        Task<int> ProcessPendingMessages(SqlConnection dbconnection, bool processAll = false, String objectID = null);
    }
}