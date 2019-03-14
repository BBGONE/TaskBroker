using TaskBroker.SSSB.EF;
using System.Threading;
using System.Threading.Tasks;

namespace TaskBroker.SSSB.Core
{
    public interface IMetaDataManager: IBaseManager
    {
        int MetaDataID { get; }
        Task<MetaData> GetMetaData(CancellationToken token = default(CancellationToken));

        CompletionResult IsAllTasksCompleted(MetaData metaData);
        Task<CompletionResult> SetCancelled();
        Task<CompletionResult> SetCompleted();
        Task<CompletionResult> SetCompletedWithError(string error);
        Task<CompletionResult> SetCompletedWithResult(string result);
    }
}