using System;
using TaskBroker.SSSB.EF;

namespace TaskBroker.SSSB.Core
{
    public class TaskInfo
    {
        public TaskInfo()
        {

        }

        public static TaskInfo FromOnDemandTask(OnDemandTask task)
        {
            TaskInfo info = new TaskInfo();
            Executor executor = task.Executor;
            info.ExecutorTypeName = executor.FullTypeName;
            info.ExecutorType = Type.GetType(info.ExecutorTypeName, true);
            info.OnDemandTaskID = task.OnDemandTaskId;
            info.Name = task.Name;
            info.ExecutorID = task.ExecutorId;
            info.SheduleID = task.SheduleId;
            info.SettingID = task.SettingId;
            return info;
        }
        public int OnDemandTaskID
        {
            get;
            internal set;
        }

        public string Name
        {
            get;
            internal set;
        }

        public short ExecutorID
        {
            get;
            internal set;
        }

        public System.Nullable<int> SheduleID
        {
            get;
            internal set;
        }

        public System.Nullable<int> SettingID
        {
            get;
            internal set;
        }

        public string ExecutorTypeName
        {
            get;
            internal set;
        }

        public Type ExecutorType
        {
            get;
            internal set;
        }
    }
}
