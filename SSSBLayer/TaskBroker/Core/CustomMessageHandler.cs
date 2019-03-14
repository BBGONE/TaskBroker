using Coordinator.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TaskBroker.SSSB.MessageHandlers;
using TaskBroker.SSSB.Results;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Core
{
    public class CustomMessageHandler
    {
        private readonly ILogger _logger;
        private readonly IStandardMessageHandlers _standardMessageHandlers;

        public CustomMessageHandler(ISSSBService sssbService, IServiceProvider services, IStandardMessageHandlers standardMessageHandlers)
        {
            this.Services = services;
            this.SSSBService = sssbService;
            _standardMessageHandlers = standardMessageHandlers;
            _logger = services.GetRequiredService<ILogger<CustomMessageHandler>>();
        }

        public ISSSBService SSSBService
        {
            get;
        }

        public IServiceProvider Services
        {
            get;
        }

        public async Task HandleMessage(SqlConnection dbconnection,
            IMessageHandler<ServiceMessageEventArgs> messageHandler,
            ServiceMessageEventArgs serviceArgs)
        {
            var previousServiceArgs = serviceArgs;
            bool isSync = true;
            Task processTask = Task.FromException(new Exception($"The message: {serviceArgs.Message.MessageType} ConversationHandle: {serviceArgs.Message.ConversationHandle} is not handled"));
            try
            {
                // handle message in its own transaction
                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    serviceArgs = await messageHandler.HandleMessage(this.SSSBService, previousServiceArgs);
                    transactionScope.Complete();
                }

                isSync = serviceArgs.Completion.IsCompleted;
            }
            catch (Exception handleEx)
            {
                if (!serviceArgs.TaskCompletionSource.TrySetException(handleEx))
                {
                    _logger.LogError(ErrorHelper.GetFullMessage(handleEx));
                }
            }
            finally
            {
                processTask = this._HandleProcessingResult(dbconnection, serviceArgs, isSync);
            }

            if (isSync)
            {
                await processTask;
            }
        }

        private async Task _HandleAsyncProcessingResult(SSSBMessage message, CancellationToken token, Task<HandleMessageResult> completionTask)
        {
            token.ThrowIfCancellationRequested();
            var connectionManager = this.Services.GetRequiredService<IConnectionManager>();

            using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            using (var dbconnection = await connectionManager.CreateSSSBConnectionAsync(token))
            {
                await _HandleSyncProcessingResult(dbconnection, message, token, completionTask);

                transactionScope.Complete();
            }
        }

        private async Task _HandleSyncProcessingResult(SqlConnection dbconnection, SSSBMessage message, CancellationToken token, Task<HandleMessageResult> completionTask)
        {
            HandleMessageResult handleMessageResult = null;
            try
            {
                handleMessageResult = await completionTask;

                await handleMessageResult.Execute(dbconnection, message, token);
            }
            catch (OperationCanceledException)
            {
                await _standardMessageHandlers.EndDialogMessageWithErrorHandler(dbconnection, message, $"Operation on Service: '{message.ServiceName}', MessageType: '{message.MessageType}', ConversationHandle: '{message.ConversationHandle}', is Cancelled", 1);
            }
            catch (PPSException ex)
            {
                await _standardMessageHandlers.EndDialogMessageWithErrorHandler(dbconnection, message, $"Operation on Service: '{message.ServiceName}', MessageType: '{message.MessageType}', ConversationHandle: '{message.ConversationHandle}', ended with Error: {ex.Message}", 2);
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError(new EventId(0, message.ServiceName), ErrorHelper.GetFullMessage(ex));
                }
                finally
                {
                    await _standardMessageHandlers.EndDialogMessageWithErrorHandler(dbconnection, message, $"Operation on Service: '{message.ServiceName}', MessageType: '{message.MessageType}', ConversationHandle: '{message.ConversationHandle}', ended with Error: {ex.Message}", 3);
                }
            }
        }

        private Task _HandleProcessingResult(SqlConnection dbconnection, ServiceMessageEventArgs serviceArgs, bool isSync)
        {
            SSSBMessage message = serviceArgs.Message;
            CancellationToken token = serviceArgs.Token;

            Task processTask = serviceArgs.Completion.ContinueWith(async (antecedent) =>
            {
                try
                {
                    if (isSync)
                    {
                        await this._HandleSyncProcessingResult(dbconnection, message, token, antecedent);
                    }
                    else
                    {
                        await this._HandleAsyncProcessingResult(message, token, antecedent);
                    }
                }
                catch (OperationCanceledException)
                {
                    // NOOP
                }
                catch (PPSException)
                {
                    // Already Logged
                }
                catch (Exception ex)
                {
                    _logger.LogError(ErrorHelper.GetFullMessage(ex));
                }
            }, isSync ? TaskContinuationOptions.ExecuteSynchronously : TaskContinuationOptions.None).Unwrap();

            var disposeTask = processTask.ContinueWith((antecedent) =>
            {
                try
                {
                    serviceArgs.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ErrorHelper.GetFullMessage(ex));
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            return processTask;
        }
    }
}
