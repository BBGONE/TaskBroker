using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace Coordinator.SSSB.Utils
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
		/// Стандартная обработка ECHO сообщения
		/// </summary>
		/// <param name="receivedMessage"></param>
		public Task EchoMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage)
        {
            return _serviceBrokerHelper.SendMessage(dbconnection, receivedMessage);
        }

        /// <summary>
        /// Стандартная обработка сообщения об ошибке
        /// </summary>
        /// <param name="message"></param>
        public async Task ErrorMessageHandler(SqlConnection dbconnection, SSSBMessage message)
        {
            try
            {
                _logger.LogError($"ErrorMessageHandler: {message.ConversationHandle} {message.MessageType} {message.ServiceName} {message.GetMessageXML()}");
            }
            finally
            {
                await _serviceBrokerHelper.EndConversation(dbconnection, message.ConversationHandle);
            }
        }

        /// <summary>
        /// Стандартная обработка сообщения о завершении диалога
        /// </summary>
        /// <param name="receivedMessage"></param>
        public Task EndDialogMessageHandler(SqlConnection dbconnection, SSSBMessage receivedMessage)
        {
             return _serviceBrokerHelper.EndConversation(dbconnection, receivedMessage.ConversationHandle);
        }

        /// <summary>
        /// Завершение диалога с отправкой сообщения об ошибке
        /// </summary>
        /// <param name="receivedMessage"></param>
        public Task EndDialogMessageWithErrorHandler(SqlConnection dbconnection, SSSBMessage receivedMessage, string message, int errorNumber)
        {
            return _serviceBrokerHelper.EndConversationWithError(dbconnection, receivedMessage.ConversationHandle, errorNumber, message);
        }

        /// <summary>
        /// Отправка ответного сообщения о завершении задачи
        /// </summary>
        /// <param name="receivedMessage"></param>
        public async Task SendStepCompleted(SqlConnection dbconnection, Guid conversationHandle)
        {
             await _serviceBrokerHelper.SendStepCompletedMessage(dbconnection, conversationHandle);
        }

        /// <summary>
        /// Отправка пустого сообщения
        /// </summary>
        /// <param name="receivedMessage"></param>
        public async Task SendEmptyMessage(SqlConnection dbconnection, Guid conversationHandle)
        {
             await _serviceBrokerHelper.SendEmptyMessage(dbconnection, conversationHandle);
        }

        #endregion
    }
}
