using Microsoft.Extensions.DependencyInjection;
using System;
using System.Xml.Linq;
using TaskBroker.SSSB.Results;

namespace TaskBroker.SSSB.Core
{
    public static class ServiceProviderExtension
    {
        public static HandleMessageResult Noop(this IServiceProvider Services)
        {
            var res = (HandleMessageResult)Services.GetRequiredService<NoopMessageResult>();
            return res;
        }

        public static HandleMessageResult EndDialogWithError(this IServiceProvider Services, string error, int? errocode, Guid? conversationHandle = null)
        {
            EndDialogMessageResult.Args args = new EndDialogMessageResult.Args()
            {
                Error = error,
                ErrorCode = errocode,
                ConversationHandle = conversationHandle
            };
            var res = (HandleMessageResult)ActivatorUtilities.CreateInstance<EndDialogMessageResult>(Services, new object[] { args });
            return res;
        }

        public static HandleMessageResult EndDialog(this IServiceProvider Services, Guid? conversationHandle = null)
        {
            EndDialogMessageResult.Args args = new EndDialogMessageResult.Args()
            {
                ConversationHandle = conversationHandle
            };
            var res = (HandleMessageResult)ActivatorUtilities.CreateInstance<EndDialogMessageResult>(Services, new object[] { args });
            return res;
        }

        public static HandleMessageResult Defer(this IServiceProvider Services, string fromService, DateTime activationTime, int attemptNumber = 1, TimeSpan? lifeTime = null)
        {
            if (string.IsNullOrEmpty(fromService))
                throw new ArgumentNullException(nameof(fromService));

            DeferMessageResult.Args args = new DeferMessageResult.Args()
            {
                FromService = fromService,
                ActivationTime = activationTime,
                AttemptNumber = attemptNumber,
                LifeTime = lifeTime
            };

            var res = (HandleMessageResult)ActivatorUtilities.CreateInstance<DeferMessageResult>(Services, new object[] { args });
            return res;
        }

        public static HandleMessageResult EmptyReply(this IServiceProvider Services, string messageType, Guid? conversationHandle = null)
        {
            GenericMessageResult.Args args = new GenericMessageResult.Args()
            {
                ConversationHandle = conversationHandle,
                MessageType = messageType
            };
            var res = (HandleMessageResult)ActivatorUtilities.CreateInstance<GenericMessageResult>(Services, new object[] { args });
            return res;
        }

        public static HandleMessageResult ReplyWithBody(this IServiceProvider Services, string messageType, XElement xmlBody, Guid? conversationHandle = null)
        {
            GenericMessageResult.Args args = new GenericMessageResult.Args()
            {
                ConversationHandle = conversationHandle,
                MessageType = messageType,
                XmlBody= xmlBody
            };
            var res = (HandleMessageResult)ActivatorUtilities.CreateInstance<GenericMessageResult>(Services, new object[] { args });
            return res;
        }

        public static HandleMessageResult StepCompleted(this IServiceProvider Services, Guid? conversationHandle = null)
        {
            return EmptyReply(Services, SSSBMessage.PPS_StepCompleteMessageType, conversationHandle);
        }

        public static HandleMessageResult EmptyMessage(this IServiceProvider Services, Guid? conversationHandle = null)
        {
            return EmptyReply(Services, SSSBMessage.PPS_EmptyMessageType, conversationHandle);
        }

        public static HandleMessageResult CombinedResult(this IServiceProvider Services, params HandleMessageResult[] resultHandlers)
        {
            var res = (HandleMessageResult)ActivatorUtilities.CreateInstance<CombinedMessageResult>(Services, new object[] { resultHandlers });
            return res;
        }
    }
}
