using System.Threading.Tasks;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.MessageHandlers
{
    public abstract class BaseMessageHandler<T>: IMessageHandler<T>
    {
        protected object SyncRoot = new object();

        protected virtual string GetName()
        {
            return nameof(BaseMessageHandler<T>);
        }

        public abstract Task<T> HandleMessage(ISSSBService sender, T args);
    }
}
