using System;
using System.Xml.Linq;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Results;

namespace TaskBroker.SSSB.Executors
{
    public static class ExecutorExtension
    {
        public static HandleMessageResult Noop(this BaseExecutor executor)
        {
            return executor.Services.Noop();
        }

        public static HandleMessageResult EndDialogWithError(this BaseExecutor executor, string error, int? errocode, Guid? conversationHandle = null)
        {
            return executor.Services.EndDialogWithError(error, errocode, conversationHandle);
        }

        public static HandleMessageResult EndDialog(this BaseExecutor executor, Guid? conversationHandle = null)
        {
            return executor.Services.EndDialog(conversationHandle);
        }

        public static HandleMessageResult Defer(this BaseExecutor executor, string fromService, DateTime activationTime, int attemptNumber = 1, TimeSpan? lifeTime = null)
        {
            return executor.Services.Defer(fromService, activationTime, attemptNumber, lifeTime);
        }

        public static HandleMessageResult EmptyReply(this BaseExecutor executor, string messageType, Guid? conversationHandle = null)
        {
            return executor.Services.EmptyReply(messageType, conversationHandle);
        }

        public static HandleMessageResult ReplyWithBody(this BaseExecutor executor, string messageType, XElement xmlBody, Guid? conversationHandle = null)
        {
            return executor.Services.ReplyWithBody(messageType, xmlBody, conversationHandle);
        }

        public static HandleMessageResult StepCompleted(this BaseExecutor executor, Guid? conversationHandle = null)
        {
            return executor.Services.StepCompleted(conversationHandle);
        }

        public static HandleMessageResult EmptyMessage(this BaseExecutor executor, Guid? conversationHandle = null)
        {
            return executor.Services.EmptyMessage(conversationHandle);
        }

        public static HandleMessageResult CombinedResult(this BaseExecutor executor, params HandleMessageResult[] resultHandlers)
        {
            return executor.Services.CombinedResult(resultHandlers);
        }
    }
}
