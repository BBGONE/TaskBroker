using System;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Results;

namespace TaskBroker.SSSB.Executors
{
    public interface IExecutor
    {
        Guid ConversationHandle
        {
            get;
        }

        string Name
        {
            get;
        }

        bool IsAsyncProcessing
        {
            get;
        }

        Task<HandleMessageResult> ExecuteTaskAsync(CancellationToken token);
    }
}