using Coordinator.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.Utils
{
    public class PubSubHelper : IPubSubHelper
    {
        public const string SUBSCRIBER_SERVICE_NAME = "PPS_SubscriberService";
        public const string PUBLISHER_SERVICE_NAME = "PPS_PublisherService";
        public const string PUBLISH_SUBSCRIBE_CONTRACT_NAME = "PPS_PublishSubscribeContract";
        public const string PPS_SubscribeMessageType = "PPS_SubscribeMessageType";
        public const string PPS_UnSubscribeMessageType = "PPS_UnSubscribeMessageType";
        public const string PPS_HeartBeatMessageType = "PPS_HeartBeatMessageType";


        private readonly ILogger _logger;
        private readonly ISSSBManager _manager;

        public PubSubHelper(ILogger<PubSubHelper> logger, ISSSBManager manager)
        {
            _logger = logger;
            _manager = manager;
        }

        private async Task<int> _PubSub(SqlConnection dbconnection, TimeSpan lifetime, Guid initiatorConversationGroupID, string topic, string messageType, bool endDialog = false)
        {
            XElement xmessage = new XElement("message",
                new XAttribute("subscriberId", initiatorConversationGroupID),
                new XElement("topic", topic)
            );
            byte[] body = xmessage.ConvertToBytes();
            try
            {
                int result = await _manager.SendMessageWithInitiatorConversationGroup(dbconnection, SUBSCRIBER_SERVICE_NAME, PUBLISHER_SERVICE_NAME, PUBLISH_SUBSCRIBE_CONTRACT_NAME,
                    (int)lifetime.TotalSeconds, false, initiatorConversationGroupID, messageType, body, endDialog);
                return result;
            }
            catch (SqlException ex)
            {
                DBWrapperExceptionsHelper.ThrowError(ex, ServiceBrokerResources.SendMessageErrMsg, _logger);
            }
            catch (Exception ex)
            {
                throw new Exception(ServiceBrokerResources.SendMessageErrMsg, ex);
            }

            return -1;
        }

        public Task<int> Subscribe(SqlConnection dbconnection, TimeSpan lifetime, Guid initiatorConversationGroupID, string topic)
        {
            return _PubSub(dbconnection, lifetime, initiatorConversationGroupID, topic, PPS_SubscribeMessageType);
        }

        public Task<int> UnSubscribe(SqlConnection dbconnection, TimeSpan lifetime, Guid initiatorConversationGroupID, string topic = "PPS_GRACEFUL_CLOSE")
        {
            return _PubSub(dbconnection, lifetime, initiatorConversationGroupID, topic, topic == "PPS_GRACEFUL_CLOSE" ? null : PPS_UnSubscribeMessageType, topic == "PPS_GRACEFUL_CLOSE"? true: false);
        }

        public Task<int> HeartBeat(SqlConnection dbconnection, TimeSpan lifetime, Guid initiatorConversationGroupID)
        {
            return _PubSub(dbconnection, lifetime, initiatorConversationGroupID, "PPS_HEART_BEAT", PPS_HeartBeatMessageType);
        }

    }
}
