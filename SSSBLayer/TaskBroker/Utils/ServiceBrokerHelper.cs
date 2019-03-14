using Coordinator.Database;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.Utils
{
    /// <summary>
    /// Вспомогательный класс для работы с SQL Service Broker.
    /// </summary>
    public class ServiceBrokerHelper : IServiceBrokerHelper
    {
        private readonly ILogger _logger;
        private readonly ISSSBManager _manager;

        public ServiceBrokerHelper(ILogger<ServiceBrokerHelper> logger, ISSSBManager manager)
        {
            _logger = logger;
            _manager = manager;
        }

        [Conditional("DEBUG")]
        public void Debug(string msg)
        {
            _logger.LogInformation(msg);
        }

        /// <summary>
        /// Запуск диалога обмена сообщениями.
        /// </summary>
        /// <param name="fromService"></param>
        /// <param name="toService"></param>
        /// <param name="contractName"></param>
        /// <param name="lifetime"></param>
        /// <param name="withEncryption"></param>
        /// <param name="relatedConversationHandle"></param>
        /// <param name="relatedConversationGroupID"></param>
        /// <returns></returns>
        public async Task<Guid> BeginDialogConversation(SqlConnection dbconnection, string fromService, string toService, string contractName, 
			TimeSpan lifetime, bool withEncryption,	Guid? relatedConversationHandle, Guid? relatedConversationGroupID)
		{
			Debug("Executing method BeginDialogConversation(fromService, toService, contractName, lifetime, withEncryption, relatedConversationID, relatedConversationGroupID)");
			try
			{
                Guid? conversationHandle = await _manager.BeginDialogConversation(dbconnection, fromService, toService, contractName,
                    lifetime == TimeSpan.Zero ? (int?)null : (int)lifetime.TotalSeconds,
                    withEncryption, relatedConversationHandle, relatedConversationGroupID);

                return conversationHandle.Value;
            }
            catch (SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.BeginDialogConversationErrMsg, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ServiceBrokerResources.BeginDialogConversationErrMsg, ex);
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Завершение диалога
        /// </summary>
        /// <param name="conversationHandle"></param>
        /// <param name="withCleanup"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorDescription"></param>
        private async Task EndConversation(SqlConnection dbconnection, Guid conversationHandle, bool withCleanup, int? errorCode, string errorDescription)
		{
            Debug($"Executing method EndConversation(conversationHandle: {conversationHandle}, withCleanup: {withCleanup}, errorCode: {errorCode}, errorDescription: {errorDescription})");
			try
			{
                await _manager.EndConversation(dbconnection, conversationHandle, withCleanup, errorCode, errorDescription);
            }
            catch(SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.EndConversationErrMsg, _logger);
            }
			catch (Exception ex)
			{
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ServiceBrokerResources.EndConversationErrMsg, ex);
			}
		}

        /// <summary>
        /// Завершение диалога.
        /// </summary>
        /// <param name="conversationHandle"></param>
        public Task EndConversation(SqlConnection dbconnection, Guid conversationHandle)
		{
			return EndConversation(dbconnection, conversationHandle, false, null, null);
		}

		/// <summary>
		/// Завершение диалога с опцией CLEANUP.
		/// </summary>
		/// <param name="conversationHandle"></param>
		public Task EndConversationWithCleanup(SqlConnection dbconnection, Guid conversationHandle)
		{
			return EndConversation(dbconnection, conversationHandle, true, null, null);
		}

		/// <summary>
		/// Завершение диалога с возвратом сообщения об ошибке.
		/// </summary>
		/// <param name="conversationHandle"></param>
		/// <param name="errorCode"></param>
		/// <param name="errorDescription"></param>
		public Task EndConversationWithError(SqlConnection dbconnection, Guid conversationHandle, int errorCode, string errorDescription)
		{
			return EndConversation(dbconnection, conversationHandle, false, errorCode, errorDescription);
		}

		/// <summary>
		/// Отправка сообщения. Сообщение содержит информацию о типе, идентификатор диалога и пр. необходимую для отправки информацию.
		/// </summary>
		/// <param name="message"></param>
		public async Task SendMessage(SqlConnection dbconnection, SSSBMessage message)
		{
            Debug($"Executing method SendMessage({message.MessageType}: {message.ConversationHandle})");
			try
			{
                await _manager.SendMessage(dbconnection, message.ConversationHandle, message.MessageType, message.Body);
			}
            catch (SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.SendMessageErrMsg, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ServiceBrokerResources.SendMessageErrMsg, ex);
            }
		}

        /// <summary>
		/// Отправка сообщения. Сообщение содержит информацию о типе, идентификатор диалога и пр. необходимую для отправки информацию.
		/// </summary>
		/// <param name="message"></param>
		public async Task SendMessage(SqlConnection dbconnection, Guid conversationHandle, string messageType, XElement xmlBody = null)
        {
            Debug($"Executing method SendMessage({messageType}: {conversationHandle})");
            try
            {
                byte[] body = xmlBody?.ConvertToBytes() ?? new byte[0];
                await _manager.SendMessage(dbconnection, conversationHandle, messageType, body);
            }
            catch (SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.SendMessageErrMsg, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ServiceBrokerResources.SendMessageErrMsg, ex);
            }
        }

        /// <summary>
        /// Отправка отложенного сообщения. Сообщение содержит информацию о типе, идентификатор диалога и пр. необходимую для отправки информацию.
        /// </summary>
        /// <param name="fromService"></param>
        /// <param name="message"></param>
        /// <param name="lifetime"></param>
        /// <param name="isWithEncryption"></param>
        /// <param name="activationTime"></param>
        /// <param name="objectID"></param>
        public async Task<long?> SendPendingMessage(SqlConnection dbconnection, 
            SSSBMessage message, 
            string fromService,
            TimeSpan lifetime, 
            bool isWithEncryption, 
            DateTime activationTime, 
            string objectID,
            int attemptNumber)
		{
            Debug($"Executing method SendPendingMessage(ConversationHandle: {message.ConversationHandle}, fromService: {fromService}, activationTime: {activationTime: dd.MM.yyyy HH:mm:ss}");
			try
			{
                XElement xmessage = new XElement("DeferedMessage",
                    new XAttribute("conversationHandle", message.ConversationHandle),
                    new XAttribute("conversationGroupID", message.ConversationGroupID),
                    new XAttribute("messageType", message.MessageType),
                    new XAttribute("serviceName", message.ServiceName),
                    new XAttribute("contractName", message.ContractName),
                    new XAttribute("sequenceNumber", message.SequenceNumber),
                    new XAttribute("validationType", message.ValidationType),
                    new XAttribute("attemptNumber", attemptNumber),
                    new XElement("body", Convert.ToBase64String(message.Body))
                );

                byte[] body = xmessage.ConvertToBytes();

                long? pendingMessageID = await _manager.SendPendingMessage(
                    dbconnection, 
                    objectID, 
                    activationTime, 
                    fromService, 
                    message.ServiceName,
                    message.ContractName, 
                    (int)lifetime.TotalSeconds, 
                    isWithEncryption,
                    body,
                    "PPS_DeferedMessageType");
                return pendingMessageID;
            }
            catch (SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.PendingMessageErrMsg, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ServiceBrokerResources.PendingMessageErrMsg, ex);
            }
            return null;
		}

        public async Task<int> ProcessPendingMessages(SqlConnection dbconnection, bool processAll= false, string objectID= null)
        {
            Debug("Executing method ProcessPendingMessage(..)");
            try
            {
                return await _manager.ProcessPendingMessages(dbconnection, processAll, objectID);
            }
            catch (SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.ProcessMessagesErrMsg, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ServiceBrokerResources.ProcessMessagesErrMsg, ex);
            }
            return -1;
        }

        /// <summary>
        /// Возвращает название очереди сообщений для сервиса
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public async Task<string> GetServiceQueueName(string serviceName)
		{
            Debug($"Executing method GetServiceQueueName({serviceName})");
			try
			{
                return await _manager.GetServiceQueueName(serviceName);
            }
            catch (SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.GetServiceQueueNameErrMsg, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                throw new PPSException(ServiceBrokerResources.GetServiceQueueNameErrMsg, ex);
            }
            return null;
		}
	}
}
