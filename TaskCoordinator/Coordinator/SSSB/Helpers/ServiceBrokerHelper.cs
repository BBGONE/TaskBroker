using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using TaskCoordinator.Database;

namespace TaskCoordinator.SSSB
{
    /// <summary>
    /// ��������������� ����� ��� ������ � SQL Service Broker.
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
        /// ������ ������� ������ �����������.
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
			Debug("���������� ������ BeginDialogConversation(fromService, toService, contractName, lifetime, withEncryption, relatedConversationID, relatedConversationGroupID)");
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
        /// ���������� �������
        /// </summary>
        /// <param name="conversationHandle"></param>
        /// <param name="withCleanup"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorDescription"></param>
        private async Task EndConversation(SqlConnection dbconnection, Guid conversationHandle, bool withCleanup, int? errorCode, string errorDescription)
		{
            Debug("���������� ������ EndConversation(conversationHandle, withCleanup, errorCode, errorDescription)");
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
        /// ������� �������� ��������� �� ��������� ���������� ����
        /// </summary>
        /// <param name="conversationHandle"></param>
        public async Task SendStepCompletedMessage(SqlConnection dbconnection, Guid conversationHandle)
        {
            Debug("���������� ������ SendStepCompletedMessage");
            try
            {
                await _manager.SendMessage(dbconnection, conversationHandle, SSSBMessage.PPS_StepCompleteMessageType, new byte[0]);
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
        /// ������� ������ ���������
        /// </summary>
        /// <param name="conversationHandle"></param>
        public async Task SendEmptyMessage(SqlConnection dbconnection, Guid conversationHandle)
        {
            Debug("���������� ������ SendEmptyMessage");
            try
            {
                await _manager.SendMessage(dbconnection, conversationHandle, SSSBMessage.PPS_EmptyMessageType, new byte[0]);
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
        /// ���������� �������.
        /// </summary>
        /// <param name="conversationHandle"></param>
        public Task EndConversation(SqlConnection dbconnection, Guid conversationHandle)
		{
			return EndConversation(dbconnection, conversationHandle, false, null, null);
		}

		/// <summary>
		/// ���������� ������� � ������ CLEANUP.
		/// </summary>
		/// <param name="conversationHandle"></param>
		public Task EndConversationWithCleanup(SqlConnection dbconnection, Guid conversationHandle)
		{
			return EndConversation(dbconnection, conversationHandle, true, null, null);
		}

		/// <summary>
		/// ���������� ������� � ��������� ��������� �� ������.
		/// </summary>
		/// <param name="conversationHandle"></param>
		/// <param name="errorCode"></param>
		/// <param name="errorDescription"></param>
		public Task EndConversationWithError(SqlConnection dbconnection, Guid conversationHandle, int errorCode, string errorDescription)
		{
			return EndConversation(dbconnection, conversationHandle, false, errorCode, errorDescription);
		}

		/// <summary>
		/// �������� ���������. ��������� �������� ���������� � ����, ������������� ������� � ��. ����������� ��� �������� ����������.
		/// </summary>
		/// <param name="message"></param>
		public async Task SendMessage(SqlConnection dbconnection, SSSBMessage message)
		{
            Debug("���������� ������ SendMessage(message)");
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
        /// �������� ����������� ���������. ��������� �������� ���������� � ����, ������������� ������� � ��. ����������� ��� �������� ����������.
        /// </summary>
        /// <param name="fromService"></param>
        /// <param name="message"></param>
        /// <param name="lifetime"></param>
        /// <param name="isWithEncryption"></param>
        /// <param name="activationTime"></param>
        /// <param name="objectID"></param>
        public async Task<long?> SendPendingMessage(SqlConnection dbconnection, string fromService, SSSBMessage message, TimeSpan lifetime, bool isWithEncryption, Guid? initiatorConversationGroupID, DateTime activationTime, string objectID)
		{
            Debug("���������� ������ SendPendingMessage(..)");
			try
			{
                long? pendingMessageID = await _manager.SendPendingMessage(dbconnection, objectID, activationTime, fromService, message.ServiceName, message.ContractName, (int)lifetime.TotalSeconds, isWithEncryption, message.ConversationGroupID, message.ConversationHandle, message.Body, message.MessageType, initiatorConversationGroupID);
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
            Debug("���������� ������ ProcessPendingMessage(..)");
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
        /// ���������� �������� ������� ��������� ��� �������
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public async Task<string> GetServiceQueueName(string serviceName)
		{
            Debug("���������� ������ GetServiceQueueName(serviceName)");
			try
			{
                return await _manager.GetServiceQueueName(serviceName).ConfigureAwait(false);
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
