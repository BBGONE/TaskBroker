using Coordinator;
using Coordinator.Database;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace TaskBroker.SSSB.Core
{
    public class SSSBMessageReader : MessageReader<SSSBMessage, SqlConnection>
    {
        public const int DEFAULT_FETCH_SIZE = 1;
        private const CommandBehavior DEFAULT_COMMAND_BEHAVIOR = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;
        public static readonly TimeSpan DefaultWaitForTimeout = TimeSpan.FromSeconds(10);
        private readonly IConnectionErrorHandler _errorHandler;
        private readonly ISSSBManager _manager;
        private readonly IConnectionManager _connectionManager;
        private readonly ISSSBService _service;
        private readonly ISSSBMessageDispatcher _dispatcher;
        private readonly Guid? _conversation_group;

        public SSSBMessageReader(long taskId, Guid? conversation_group, BaseTasksCoordinator tasksCoordinator, ILogger log,
            ISSSBService service, ISSSBMessageDispatcher dispatcher, 
            IConnectionErrorHandler errorHandler, ISSSBManager manager, IConnectionManager connectionManager) :
            base(taskId, tasksCoordinator, log)
        {
            this._conversation_group = conversation_group;
            this._service = service;
            this._dispatcher = dispatcher;
            this._errorHandler = errorHandler;
            this._manager = manager;
            this._connectionManager = connectionManager;
        }

        /// <summary>
        /// Load message data from the data reader
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private SSSBMessage FillMessageFromReader(IDataReader reader)
        {
            //conversation_group_id, 
            //conversation_handle,
            //message_sequence_number, 
            //service_name, 
            //service_contract_name, 
            //message_type_name, 
            //validation, 
            //message_body 
            Guid conversationGroupID = reader.GetGuid(0);
            Guid conversationHandle = reader.GetGuid(1);
            long sequenceNumber = reader.GetInt64(2);
            string serviceName = reader.GetString(3);
            string contractName = reader.GetString(4);
            string messageType = reader.GetString(5);
            string validation = reader.GetString(6);
            MessageValidationType validationType = MessageValidationType.None;
            if (validation == "X")
                validationType = MessageValidationType.XML;
            else if (validation == "E")
                validationType = MessageValidationType.Empty;

            SSSBMessage message = new SSSBMessage(conversationHandle, conversationGroupID, validationType, contractName);
            message.SequenceNumber = sequenceNumber;
            message.ServiceName = serviceName;
            message.MessageType = messageType;

            if (!reader.IsDBNull(7))
            {
                /*
                byte[] buffer = new byte[8040];
                long offset = 0;
                int read;
                using (MemoryStream ms = new MemoryStream())
                {
                    while ((read = (int)reader.GetBytes(7, offset, buffer, 0, buffer.Length)) > 0)
                    {
                        offset += read;
                        ms.Write(buffer, 0, read);
                    }
                    ms.Flush();
                    ms.Position = 0;
                    message.Body = ms.ToArray();
                }
                */

                /*
                    // Get message size
                    int size = (int)reader.GetBytes(7, 0, null, 0, 0);
                    message.Body = new byte[size];
                    reader.GetBytes(7, 0, message.Body, 0, message.Body.Length);
               */

                message.Body = (byte[])reader[7];
            }
            else
            {
                message.Body = null;
            }

            message.ServiceName = this._service.Name;

            return message;
        }

        protected override async Task<SSSBMessage> ReadMessage(bool isPrimaryReader, long taskId, CancellationToken token, SqlConnection state)
        {
            SqlConnection dbconnection = state;
            // reading messages from the queue
            IDataReader reader = null;
            try
            {
                if (isPrimaryReader)
                    reader = await _manager.ReceiveMessagesAsync(dbconnection, this._service.QueueName,
                        DEFAULT_FETCH_SIZE,
                        (int)DefaultWaitForTimeout.TotalMilliseconds,
                        _conversation_group,
                        DEFAULT_COMMAND_BEHAVIOR,
                        token);
                else
                    reader = await _manager.ReceiveMessagesNoWaitAsync(dbconnection, this._service.QueueName,
                        DEFAULT_FETCH_SIZE,
                        _conversation_group,
                        DEFAULT_COMMAND_BEHAVIOR,
                        token);

                // no result after cancellation
                if (reader == null)
                {
                    return null;
                }

                return reader.Read() ? this.FillMessageFromReader(reader) : null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (PPSException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var exception = new PPSException(string.Format("ReadMessages error on queue: {0}, isPrimaryListener: {1}", this._service.QueueName, isPrimaryReader), ex);
                Log.LogError(ErrorHelper.GetFullMessage(exception));
                throw exception;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        protected override async Task<MessageProcessingResult> DispatchMessage(SSSBMessage message, long taskId, CancellationToken token, SqlConnection state)
        {
            var res = await this._dispatcher.DispatchMessage(message, taskId, token, state);
            return res;
        }

        protected async Task<SqlConnection> TryGetConnection(CancellationToken token)
        {
            SqlConnection dbconnection = null;
            Exception error = null;
            try
            {
                dbconnection = await _connectionManager.CreateSSSBConnectionAsync(token);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                await _errorHandler.Handle(error, token);
                throw error;
            }

            return dbconnection;
        }

        protected override async Task<int> DoWork(bool isPrimaryReader, CancellationToken token)
        {
            int cnt = 0;
            SSSBMessage msg = null;
            SqlConnection dbconnection = null;
            TransactionScope transactionScope = null;

            var disposable = this.Coordinator.ReadThrottle(isPrimaryReader);
            try
            {
                TransactionOptions tranOptions = new TransactionOptions();
                tranOptions.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
                tranOptions.Timeout = TimeSpan.FromMinutes(60);
                transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, tranOptions, TransactionScopeAsyncFlowOption.Enabled);
            }
            catch
            {
                disposable.Dispose();
                throw;
            }

            using (transactionScope)
            {
                try
                {
                    dbconnection = await this.TryGetConnection(token);
                    msg = await this.ReadMessage(isPrimaryReader, this.taskId, token, dbconnection);
                }
                finally
                {
                    disposable.Dispose();
                }

                cnt = msg == null ? 0 : 1;

                using (dbconnection)
                {
                    if (msg != null)
                    {
                        this.Coordinator.OnBeforeDoWork(this);
                        try
                        {
                            MessageProcessingResult res = await this.DispatchMessage(msg, this.taskId, token, dbconnection);
                            if (res.isRollBack)
                            {
                                await this.OnRollback(msg, token);
                                return cnt;
                            }
                        }
                        catch (Exception ex)
                        {
                            this.OnProcessMessageException(ex, msg);
                            throw;
                        }
                        finally
                        {
                            this.Coordinator.OnAfterDoWork(this);
                        }

                        transactionScope.Complete();
                    }
                }
            }

            return cnt;
        }

        protected override void OnProcessMessageException(Exception ex, SSSBMessage msg)
        {
            if (msg != null)
            {
                this._service.AddError(msg.ConversationHandle, ex);
            }
        }
    }
}