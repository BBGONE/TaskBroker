using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.MessageHandlers
{
    public class DeferedMessageHandler: TaskMessageHandler
    {
        public DeferedMessageHandler(IServiceProvider rootServices):
            base(rootServices)
        {
        }

        protected override string GetName()
        {
            return nameof(DeferedMessageHandler);
        }


        public override async Task<ServiceMessageEventArgs> HandleMessage(ISSSBService sender, ServiceMessageEventArgs serviceMessageArgs)
        {
            MessageAtributes messageAtributes = null;
            SSSBMessage originalMessage = null;
            try
            {
                serviceMessageArgs.Token.ThrowIfCancellationRequested();
                XElement envelopeXml = serviceMessageArgs.Message.GetMessageXML();
                byte[] originalMessageBody = Convert.FromBase64String(envelopeXml.Element("body").Value);
                XElement originalMessageXml = originalMessageBody.GetMessageXML();
                messageAtributes = originalMessageXml.GetMessageAttributes();
                messageAtributes.IsDefered = true;
                messageAtributes.AttemptNumber = (int)envelopeXml.Attribute("attemptNumber");
                string messageType = (string)envelopeXml.Attribute("messageType");
                string serviceName = (string)envelopeXml.Attribute("serviceName");
                string contractName = (string)envelopeXml.Attribute("contractName");
                long sequenceNumber = (long)envelopeXml.Attribute("sequenceNumber");
                MessageValidationType validationType = (MessageValidationType)Enum.Parse(typeof(MessageValidationType), envelopeXml.Attribute("validationType").Value);
                Guid conversationHandle = Guid.Parse(envelopeXml.Attribute("conversationHandle").Value);
                Guid conversationGroupID = Guid.Parse(envelopeXml.Attribute("conversationGroupID").Value);

                originalMessage = new SSSBMessage(conversationHandle, conversationGroupID, validationType, contractName)
                {
                    SequenceNumber = sequenceNumber,
                    ServiceName = serviceName,
                    Body = originalMessageBody
                };
            }
            catch (OperationCanceledException)
            {
                serviceMessageArgs.TaskCompletionSource.TrySetCanceled(serviceMessageArgs.Token);
                return serviceMessageArgs;
            }
            catch (PPSException ex)
            {
                serviceMessageArgs.TaskCompletionSource.TrySetException(ex);
                return serviceMessageArgs;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ErrorHelper.GetFullMessage(ex));
                serviceMessageArgs.TaskCompletionSource.TrySetException(new PPSException(ex));
                return serviceMessageArgs;
            }

            OnDemandTaskManager taskManager = serviceMessageArgs.Services.GetRequiredService<OnDemandTaskManager>();
            try
            {
                serviceMessageArgs.Token.ThrowIfCancellationRequested();
                var task = await taskManager.GetTaskInfo(messageAtributes.TaskID.Value);
                serviceMessageArgs.TaskID = messageAtributes.TaskID.Value;
                var executorArgs = new ExecutorArgs(taskManager, task, originalMessage, messageAtributes);
                await ExecuteTask(executorArgs, serviceMessageArgs);
            }
            catch (OperationCanceledException)
            {
                serviceMessageArgs.TaskCompletionSource.TrySetCanceled(serviceMessageArgs.Token);
            }
            catch (PPSException ex)
            {
                serviceMessageArgs.TaskCompletionSource.TrySetException(ex);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ErrorHelper.GetFullMessage(ex));
                serviceMessageArgs.TaskCompletionSource.TrySetException(new PPSException(ex));
            }

            return serviceMessageArgs;
        }
    }
}
