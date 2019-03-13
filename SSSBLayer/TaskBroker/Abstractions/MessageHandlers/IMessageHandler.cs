using System.Threading.Tasks;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.MessageHandlers
{
    public interface IMessageHandler<T>
    {
        Task<T> HandleMessage(ISSSBService sender, T e);
    }
}
