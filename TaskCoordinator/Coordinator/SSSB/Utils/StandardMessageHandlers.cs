using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace TaskCoordinator.SSSB.Utils
{
    public class StandardMessageHandlers : IStandardMessageHandlers
    {
        private readonly ILogger _logger;
        private readonly IServiceBrokerHelper _serviceBrokerHelper;

        public StandardMessageHandlers(ILogger<StandardMessageHandlers> logger, IServiceBrokerHelper serviceBrokerHelper)
        {
            _logger = logger;
            _serviceBrokerHelper = serviceBrokerHelper;
        }

        #region Standard MessageHandlers
        /// <summary>
		/// ����������� ��������� ECHO ���������
		/// </summary>
		/// <param name="receivedMessage"></param>
		public Task EchoMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage)
        {
            return _serviceBrokerHelper.SendMessage(dbconnection, receivedMessage);
        }

        /// <summary>
        /// ����������� ��������� ��������� �� ������
        /// </summary>
        /// <param name="receivedMessage"></param>
        public async Task ErrorMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage)
        {
            await _serviceBrokerHelper.EndConversation(dbconnection, receivedMessage.ConversationHandle);
            _logger.LogError(string.Format(ServiceBrokerResources.ErrorMessageReceivedErrMsg, receivedMessage.ConversationHandle, Encoding.Unicode.GetString(receivedMessage.Body)));
        }

        /// <summary>
        /// ����������� ��������� ��������� � ���������� �������
        /// </summary>
        /// <param name="receivedMessage"></param>
        public Task EndDialogMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage)
        {
             return _serviceBrokerHelper.EndConversation(dbconnection, receivedMessage.ConversationHandle);
        }

        /// <summary>
        /// ���������� ������� � ��������� ��������� �� ������
        /// </summary>
        /// <param name="receivedMessage"></param>
        public Task EndDialogMessageWithErrorHandler(SqlConnection dbconnection, SSSBMessage receivedMessage, string message, int errorNumber)
        {
            return _serviceBrokerHelper.EndConversationWithError(dbconnection, receivedMessage.ConversationHandle, errorNumber, message);
        }

        /// <summary>
        /// �������� ��������� ��������� � ���������� ������
        /// </summary>
        /// <param name="receivedMessage"></param>
        public async Task SendStepCompleted(SqlConnection dbconnection, Guid conversationHandle)
        {
             await _serviceBrokerHelper.SendStepCompletedMessage(dbconnection, conversationHandle);
        }

        /// <summary>
        /// �������� ������� ���������
        /// </summary>
        /// <param name="receivedMessage"></param>
        public async Task SendEmptyMessage(SqlConnection dbconnection, Guid conversationHandle)
        {
             await _serviceBrokerHelper.SendEmptyMessage(dbconnection, conversationHandle);
        }

        #endregion
    }
}
