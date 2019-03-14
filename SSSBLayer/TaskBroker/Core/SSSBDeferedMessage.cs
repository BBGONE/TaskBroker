using System;

namespace TaskBroker.SSSB.Core
{
	[Serializable]
	public class SSSBDeferedMessage : SSSBMessage
    {
        public SSSBDeferedMessage(Guid conversationHandle, Guid conversationGroupID, MessageValidationType validationType, string contractName, SSSBMessage parentMessage):
            base(conversationHandle, conversationGroupID, validationType, contractName)
        {
            ParentMessage = parentMessage;
        }

        public SSSBMessage ParentMessage
        {
            get;
		}
	}
}
