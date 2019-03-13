namespace TaskBroker.SSSB.Core
{
    public class ExecutorArgs
    {
        public ExecutorArgs(OnDemandTaskManager tasksManager, TaskInfo taskInfo, SSSBMessage message, MessageAtributes messageAtributes)
        {
            this.TasksManager = tasksManager;
            this.TaskInfo = taskInfo;
            this.Message = message;
            this.Atributes = messageAtributes;
        }

        public OnDemandTaskManager TasksManager { get; }
        public TaskInfo TaskInfo { get; }
        public MessageAtributes Atributes { get; }
        public SSSBMessage Message { get; }
    }
}
