using Coordinator.Database;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Results;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.MessageHandlers
{
    public class CustomMessageHandler : ICustomMessageHandler
    {
        private readonly ILogger _logger;
        private readonly IStandardMessageHandlers _standardMessageHandlers;
        private readonly IConnectionManager _connectionManager;

        public CustomMessageHandler(ISSSBService sssbService, IConnectionManager connectionManager, IStandardMessageHandlers standardMessageHandlers, ILogger<CustomMessageHandler> logger)
        {
            _connectionManager = connectionManager;
            SSSBService = sssbService;
            _standardMessageHandlers = standardMessageHandlers;
            _logger = logger;
        }

        public ISSSBService SSSBService
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
            // handle message in its own transaction
            using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                serviceArgs = await messageHandler.HandleMessage(this.SSSBService, previousServiceArgs);
                transactionScope.Complete();
            }

            isSync = serviceArgs.Completion.IsCompleted;
            processTask = this._HandleProcessingResult(dbconnection, serviceArgs, isSync);

            if (isSync)
            {
                await processTask;
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
                        await this.HandleSyncProcessingResult(dbconnection, message, token, antecedent);
                    }
                    else
                    {
                        await this.HandleAsyncProcessingResult(message, token, antecedent);
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
                    this.OnDispose(serviceArgs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ErrorHelper.GetFullMessage(ex));
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            return processTask;
        }

        #region virtual methods
        protected virtual async Task HandleAsyncProcessingResult(SSSBMessage message, CancellationToken token, Task<HandleMessageResult> completionTask)
        {
            token.ThrowIfCancellationRequested();

            using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            using (var dbconnection = await _connectionManager.CreateSSSBConnectionAsync(token))
            {
                await HandleSyncProcessingResult(dbconnection, message, token, completionTask);

                transactionScope.Complete();
            }
        }

        protected virtual async Task HandleSyncProcessingResult(SqlConnection dbconnection, SSSBMessage message, CancellationToken token, Task<HandleMessageResult> completionTask)
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

        protected virtual void OnDispose(ServiceMessageEventArgs serviceArgs)
        {
            serviceArgs.Dispose();
        }
        #endregion
    }
}
