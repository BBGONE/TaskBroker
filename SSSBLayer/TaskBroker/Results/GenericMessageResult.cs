using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Results
{
    public class GenericMessageResult : HandleMessageResult
    {
        private readonly IServiceBrokerHelper _serviceBrokerHelper;
        private readonly Guid? _conversationHandle;
        private readonly string _messageType;
        private readonly XElement _xmlBody;

        public class Args
        {
            public Guid? ConversationHandle { get; set; }
            public string MessageType { get; set; }
            public XElement XmlBody { get; set; }
    }

        public GenericMessageResult(IServiceBrokerHelper serviceBrokerHelper, Args args)
        {
            _serviceBrokerHelper = serviceBrokerHelper;
            _conversationHandle = args.ConversationHandle;
            _messageType = args.MessageType;
            _xmlBody = args.XmlBody;
        }

        public override Task Execute(SqlConnection dbconnection, SSSBMessage message, CancellationToken token)
        {
            Guid conversationHandle = _conversationHandle.HasValue ? _conversationHandle.Value : message.ConversationHandle;
            return _serviceBrokerHelper.SendMessage(dbconnection, conversationHandle, _messageType, _xmlBody);
        }
    }
}
