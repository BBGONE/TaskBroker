using System;
using System.Threading.Tasks;
using TaskBroker.SSSB.Executors;

namespace TaskBroker.SSSB.Core
{
    public interface IOnDemandTaskManager: IBaseManager
    {
        IExecutor CurrentExecutor { get; set; }
        Guid ID { get; }
        ISettingsService Settings { get; }

        Task<TaskInfo> GetTaskInfo(int id);
    }
}