using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Scheduler;

namespace TaskBroker.SSSB.MessageHandlers
{
    public class TaskEndedMessageHandler : BaseMessageHandler<ServiceMessageEventArgs>
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;

        public TaskEndedMessageHandler(IServiceProvider services)
        {
            _logger = services.GetRequiredService<ILogger<TaskEndedMessageHandler>>();
            _services = services;
        }

        protected override string GetName()
        {
            return nameof(TaskEndedMessageHandler);
        }

        public override Task<ServiceMessageEventArgs> HandleMessage(ISSSBService sender, ServiceMessageEventArgs args)
        {
            try
            {
                // after the task is completed, to turn on the Timer again
                lock (ScheduleTimer.Timers)
                {
                    Guid conversationGroup = args.Message.ConversationGroupID;
                    var timer = (from t in BaseSheduleTimer.Timers.Values
                                 where t.ConversationGroup.HasValue && t.ConversationGroup.Value == conversationGroup
                                 select t).SingleOrDefault();

                    if (timer != null)
                    {
                        timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
            finally
            {
                args.TaskCompletionSource.SetResult(_services.EndDialog());
            }

            return Task.FromResult(args);
        }
    }
}
