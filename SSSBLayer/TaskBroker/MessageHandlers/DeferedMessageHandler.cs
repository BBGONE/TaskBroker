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
            SSSBMessage deferedMessage = null;
            ServiceMessageEventArgs previousServiceMessageArgs = serviceMessageArgs;
            var tcs = serviceMessageArgs.TaskCompletionSource;
            try
            {
                serviceMessageArgs.Token.ThrowIfCancellationRequested();
                XElement envelopeXml = serviceMessageArgs.Message.GetMessageXML();
                byte[] deferedMessageBody = Convert.FromBase64String(envelopeXml.Element("body").Value);
                XElement deferedMessageXml = deferedMessageBody.GetMessageXML();
                messageAtributes = deferedMessageXml.GetMessageAttributes();
                messageAtributes.IsDefered = true;
                messageAtributes.AttemptNumber = (int)envelopeXml.Attribute("attemptNumber");
                string messageType = (string)envelopeXml.Attribute("messageType");
                string serviceName = (string)envelopeXml.Attribute("serviceName");
                string contractName = (string)envelopeXml.Attribute("contractName");
                long sequenceNumber = (long)envelopeXml.Attribute("sequenceNumber");
                MessageValidationType validationType = (MessageValidationType)Enum.Parse(typeof(MessageValidationType), envelopeXml.Attribute("validationType").Value);
                Guid conversationHandle = Guid.Parse(envelopeXml.Attribute("conversationHandle").Value);
                Guid conversationGroupID = Guid.Parse(envelopeXml.Attribute("conversationGroupID").Value);

                deferedMessage = new SSSBDeferedMessage(conversationHandle, conversationGroupID, validationType, contractName, previousServiceMessageArgs.Message)
                {
                    SequenceNumber = sequenceNumber,
                    ServiceName = serviceName,
                    Body = deferedMessageBody
                };

                serviceMessageArgs = new ServiceMessageEventArgs(previousServiceMessageArgs, deferedMessage);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled(serviceMessageArgs.Token);
                return serviceMessageArgs;
            }
            catch (PPSException ex)
            {
                tcs.TrySetException(ex);
                return serviceMessageArgs;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ErrorHelper.GetFullMessage(ex));
                tcs.TrySetException(new PPSException(ex));
                return serviceMessageArgs;
            }

            var taskManager = serviceMessageArgs.Services.GetRequiredService<IOnDemandTaskManager>();
            try
            {
                serviceMessageArgs.Token.ThrowIfCancellationRequested();
                var task = await taskManager.GetTaskInfo(messageAtributes.TaskID.Value);
                serviceMessageArgs.TaskID = messageAtributes.TaskID.Value;
                var executorArgs = new ExecutorArgs(taskManager, task, deferedMessage, messageAtributes);
                await ExecuteTask(executorArgs, serviceMessageArgs);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled(serviceMessageArgs.Token);
            }
            catch (PPSException ex)
            {
                tcs.TrySetException(ex);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ErrorHelper.GetFullMessage(ex));
                tcs.TrySetException(new PPSException(ex));
            }

            return serviceMessageArgs;
        }
    }
}
