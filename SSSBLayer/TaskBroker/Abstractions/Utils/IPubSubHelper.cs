using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TaskBroker.SSSB.Utils
{
    public interface IPubSubHelper
    {
        Task<int> HeartBeat(SqlConnection dbconnection, TimeSpan lifetime, Guid initiatorConversationGroupID);
        Task<int> Subscribe(SqlConnection dbconnection, TimeSpan lifetime, Guid initiatorConversationGroupID, string topic);
        Task<int> UnSubscribe(SqlConnection dbconnection, TimeSpan lifetime, Guid initiatorConversationGroupID, string topic = "PPS_GRACEFUL_CLOSE");
    }
}