using Coordinator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using SSSBLayer.TaskBroker.Factories;
using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TaskBroker.SSSB.Errors;
using TaskBroker.SSSB.MessageHandlers;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Core
{
    public class SSSBMessageDispatcher : ISSSBMessageDispatcher
    {
        private readonly ILogger _logger;
        private readonly ISSSBService _sssbService;
        private readonly ConcurrentDictionary<string, IMessageHandler<ServiceMessageEventArgs>> _messageHandlers;
        private readonly ConcurrentDictionary<string, IMessageHandler<ErrorMessageEventArgs>> _errorMessageHandlers;
        private readonly IStandardMessageHandlers _standardMessageHandlers;
        private readonly IServiceProvider _services;

        #region  Constants
        public const int MAX_MESSAGE_ERROR_COUNT = 2;
        /// <summary>
        /// The system defined contract name for echo.
        /// </summary>
        private const string EchoContractName = "http://schemas.microsoft.com/SQL/ServiceBroker/ServiceEcho";
        #endregion

        public SSSBMessageDispatcher(ISSSBService sssbService, ILogger<SSSBMessageDispatcher> logger, IStandardMessageHandlers standardMessageHandlers, IServiceProvider services)
        {
            this._logger = logger;
            this._sssbService = sssbService;
            this._standardMessageHandlers = standardMessageHandlers;
            this._services = services;
            this._messageHandlers = new ConcurrentDictionary<string, IMessageHandler<ServiceMessageEventArgs>>();
            this._errorMessageHandlers = new ConcurrentDictionary<string, IMessageHandler<ErrorMessageEventArgs>>();
        }

        protected virtual ServiceMessageEventArgs CreateServiceMessageEventArgs(SSSBMessage message, CancellationToken cancellation)
        {
            ServiceMessageEventArgs args = new ServiceMessageEventArgs(message, this._sssbService, cancellation, _services.CreateScope());
            return args;
        }

        private async Task DispatchErrorMessage(SqlConnection dbconnection, SSSBMessage message, ErrorMessage msgerr, CancellationToken token)
        {
            try
            {
                // для каждого типа сообщения можно добавить нестандартную обработку 
                // которое не может быть обработано
                // например: сохранить тело сообщения в логе
                IMessageHandler<ErrorMessageEventArgs> errorMessageHandler;

                if (_errorMessageHandlers.TryGetValue(message.MessageType, out errorMessageHandler))
                {
                    using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        ErrorMessageEventArgs errArgs = new ErrorMessageEventArgs(message, this._sssbService, msgerr.FirstError, token);
                        errArgs = await errorMessageHandler.HandleMessage(this._sssbService, errArgs);

                        transactionScope.Complete();
                    }
                }

                await _standardMessageHandlers.EndDialogMessageWithErrorHandler(dbconnection, message, msgerr.FirstError.Message, 4);

                string error = string.Format("Message {0} caused MAX Number of errors '{1}'. Dialog aborted!", message.MessageType, msgerr.FirstError.Message);
                _logger.LogError(error);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ErrorHelper.GetFullMessage(ex));
            }
        }

        private Func<Task> _GetMessageHandler(SqlConnection dbconnection, SSSBMessage message, CancellationToken token)
        {
            IMessageHandler<ServiceMessageEventArgs> messageHandler;

            // if we registered custom handlers for predefined message types
            if (_messageHandlers.TryGetValue(message.MessageType, out messageHandler))
            {
                return async () =>
                {
                    ServiceMessageEventArgs serviceArgs = this.CreateServiceMessageEventArgs(message, token);
                    try
                    {
                        var customHandlerFactory = serviceArgs.Services.GetRequiredService<ICustomMessageHandlerFactory>();
                        var customHandler = customHandlerFactory.Create(serviceArgs);
                        await customHandler.HandleMessage(dbconnection, messageHandler, serviceArgs);
                    }
                    catch (Exception ex)
                    {
                        serviceArgs.TaskCompletionSource.TrySetException(ex);
                        throw;
                    }
                };
            }
            else if (message.MessageType == SSSBMessage.EndDialogMessageType)
            {
                return () => _standardMessageHandlers.EndDialogMessageHandler(dbconnection, message);
            }
            else if (message.MessageType == SSSBMessage.ErrorMessageType)
            {
                return () => _standardMessageHandlers.ErrorMessageHandler(dbconnection, message);
            }
            else if (message.MessageType == SSSBMessage.EchoMessageType && message.ContractName == EchoContractName)
            {
                return () => _standardMessageHandlers.EchoMessageHandler(dbconnection, message);
            }
            else
            {
                string err = string.Format(ServiceBrokerResources.UnknownMessageTypeErrMsg, message.MessageType);
                return () => _standardMessageHandlers.EndDialogMessageWithErrorHandler(dbconnection, message, err, 1);
            }
        }

        private async Task<bool> _DispatchMessage(SqlConnection dbconnection, SSSBMessage message, CancellationToken token)
        {
            bool rollBack = false;

            try
            {
                var handler = _GetMessageHandler(dbconnection, message, token);
                await handler();
            }
            catch(Exception ex)
            {
                try
                {
                    _logger.LogError(ex.GetFullMessage());
                    await _standardMessageHandlers.EndDialogMessageWithErrorHandler(dbconnection, message, ex.GetFullMessage(false), 1);
                }
                catch(Exception ex2)
                {
                    rollBack = true;
                    _logger.LogError(ex2.GetFullMessage());
                }
            }

            return rollBack;
        }

        async Task<MessageProcessingResult> IMessageDispatcher<SSSBMessage, SqlConnection>.DispatchMessage(SSSBMessage message, long taskId, CancellationToken token, SqlConnection dbconnection)
        {
            bool rollBack = false;

            ErrorMessage msgerr = null;
            bool end_dialog_with_error = false;
            // определяем сообщение по ConversationHandle
            // оканчивалась ли ранее обработка этого сообщения с ошибкой?
            msgerr = _sssbService.GetError(message.ConversationHandle);
            if (msgerr != null)
                end_dialog_with_error = msgerr.ErrorCount >= MAX_MESSAGE_ERROR_COUNT;

            if (end_dialog_with_error)
                await this.DispatchErrorMessage(dbconnection, message, msgerr, token);
            else
                rollBack = await this._DispatchMessage(dbconnection, message, token);

            return new MessageProcessingResult() { isRollBack = rollBack };
        }

        public void RegisterMessageHandler(string messageType, IMessageHandler<ServiceMessageEventArgs> handler)
        {
            _messageHandlers[messageType] = handler;
        }

        public void RegisterErrorMessageHandler(string messageType, IMessageHandler<ErrorMessageEventArgs> handler)
        {
            _errorMessageHandlers[messageType] = handler;
        }

        public void UnregisterMessageHandler(string messageType)
        {
            _messageHandlers.TryRemove(messageType, out var _);
        }

        public void UnregisterErrorMessageHandler(string messageType)
        {
            _errorMessageHandlers.TryRemove(messageType, out var _);
        }
    }
}
