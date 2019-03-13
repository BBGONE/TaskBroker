using System.Threading.Tasks;

namespace TaskBroker.SSSB.Core
{
    public interface ISettingsService
    {
        Task<T> GetByID<T>(int settingID) where T : class;
        Task<T> GetByTaskID<T>(int taskID) where T : class;
        Task<ExecutorSettings> GetByID(int settingID);
        Task<ExecutorSettings> GetByTaskID(int taskID);
    }
}