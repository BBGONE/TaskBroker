namespace TaskBroker.SSSB.Core
{
    public class ExecutorArgs
    {
        public ExecutorArgs(IOnDemandTaskManager tasksManager, TaskInfo taskInfo, SSSBMessage message, MessageAtributes messageAtributes)
        {
            this.TasksManager = tasksManager;
            this.TaskInfo = taskInfo;
            this.Message = message;
            this.Atributes = messageAtributes;
        }

        public IOnDemandTaskManager TasksManager { get; }
        public TaskInfo TaskInfo { get; }
        public MessageAtributes Atributes { get; }
        public SSSBMessage Message { get; }
    }
}
