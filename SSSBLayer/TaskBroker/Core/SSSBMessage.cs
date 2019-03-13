using Coordinator;
using System;

namespace TaskBroker.SSSB.Core
{
    [Serializable]
	public enum MessageValidationType
	{
		None,

		Empty,

		XML,
	}

	[Serializable]
	public class SSSBMessage : Message
    {
		#region WellKnown MesssageTypes

		/// <value>
		/// System message type for event notification messages.
		/// </value>
		public const string EventNotificationMessageType = "http://schemas.microsoft.com/SQL/Notifications/EventNotification";

		/// <value>
		/// System message type for query notification messages.
		/// </value>
		public const string QueryNotificationMessageType = "http://schemas.microsoft.com/SQL/Notifications/QueryNotification";

		/// <value>
		/// System message type for message indicating failed remote service binding.
		/// </value>
		public const string FailedRemoteServiceBindingMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/BrokerConfigurationNotice/FailedRemoteServiceBinding";

		/// <value>
		/// System message type for message indicating failed route.
		/// </value>
		public const string FailedRouteMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/BrokerConfigurationNotice/FailedRoute";

		/// <value>
		/// System message type for message indicating missing remote service binding.
		/// </value>
		public const string MissingRemoteServiceBindingMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/BrokerConfigurationNotice/MissingRemoteServiceBinding";

		/// <value>
		/// System message type for message indicating missing route.
		/// </value>
		public const string MissingRouteMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/BrokerConfigurationNotice/MissingRoute";

		/// <value>
		/// System message type for dialog timer messages.
		/// </value>
		public const string DialogTimerMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer";

		/// <value>
		/// System message type for message indicating end of dialog.
		/// </value>
		public const string EndDialogMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog";

		/// <value>
		/// System message type for error messages.
		/// </value>
		public const string ErrorMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/Error";

		/// <value>
		/// System message type for diagnostic description messages.
		/// </value>
		public const string DescriptionMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/ServiceDiagnostic/Description";

		/// <value>
		/// System message type for diagnostic query messages.
		/// </value>
		public const string QueryMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/ServiceDiagnostic/Query";

		/// <value>
		/// System message type for diagnostic status messages.
		/// </value>
		public const string StatusMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/ServiceDiagnostic/Status";

		/// <value>
		/// System message type for echo service messages.
		/// </value>
		public const string EchoMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/ServiceEcho/Echo";

        /// <value>
        /// Empty message type for service awakening from waiting new message
        /// </value>
        public const string DefaultMessageType = "DEFAULT";
		# endregion

        public const string PPS_EmptyMessageType = "PPS_EmptyMessageType";
        public const string PPS_StepCompleteMessageType = "PPS_StepCompleteMessageType";

        public SSSBMessage(Guid conversationHandle, Guid conversationGroupID, MessageValidationType validationType, string contractName)
        {
            ConversationHandle = conversationHandle;
            ConversationGroupID = conversationGroupID;
            ValidationType = validationType;
            ContractName = contractName;
        }

        /// <summary>
        /// Идентификатор диалога обмена сообщениями.
        /// </summary>
        public Guid ConversationHandle
		{
            get;
		}

		/// <summary>
		/// Идентификатор группы сообщений.
		/// </summary>
		public Guid ConversationGroupID
		{
            get;
		}
	
		/// <summary>
		/// Тип валидации сообщения.
		/// </summary>
		public MessageValidationType ValidationType
		{
            get;
		}

		/// <summary>
		/// Контракт, в рамках которого было отправлено сообщение.
		/// </summary>
		public string ContractName
		{
            get;
		}
	}
}
