using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Coordinator
{
    public abstract class MessageReader<TMessage, TState> : IMessageReader
        where TMessage:class
    {
        #region Private Fields
        private long _taskId;
        private readonly ITaskCoordinatorAdvanced _coordinator;
        private readonly ILogger _log;
        #endregion

        public MessageReader(long taskId, ITaskCoordinatorAdvanced tasksCoordinator, ILogger log)
        {
            this._taskId = taskId;
            this._coordinator = tasksCoordinator;
            this._log = log;
        }

        protected abstract Task<TMessage> ReadMessage(bool isPrimaryReader, long taskId, CancellationToken token, TState state);

        protected abstract Task<MessageProcessingResult> DispatchMessage(TMessage message, long taskId, CancellationToken token, TState state);
      
        protected abstract Task<int> DoWork(bool isPrimaryReader, CancellationToken cancellation);

        protected virtual Task OnRollback(TMessage msg, CancellationToken token)
        {
            return Task.FromResult(0);
        }
    
        protected virtual void OnProcessMessageException(Exception ex, TMessage message)
        {
            // NOOP
        }

        async Task<MessageReaderResult> IMessageReader.TryProcessMessage(CancellationToken token)
        {
            if (this._coordinator.IsPaused)
            {
                await Task.Delay(1000, token);
                return new MessageReaderResult() { IsWorkDone = true, IsRemoved = false };
            }
            token.ThrowIfCancellationRequested();
            bool isDidWork = await this.DoWork(this.IsPrimaryReader, token) > 0;

            return this.AfterProcessedMessage(isDidWork, token);
        }

        protected MessageReaderResult AfterProcessedMessage(bool workDone, CancellationToken token)
        {
            bool isRemoved = token.IsCancellationRequested || this._coordinator.IsSafeToRemoveReader(this, workDone);
            return new MessageReaderResult() { IsRemoved = isRemoved, IsWorkDone = workDone };
        }

        protected ILogger Log
        {
            get { return _log; }
        }

        public long taskId
        {
            get
            {
                return this._taskId;
            }
        }

        public bool IsPrimaryReader
        {
            get
            {
                return this._coordinator.IsPrimaryReader(this);
            }
        }

        public ITaskCoordinatorAdvanced Coordinator {
            get
            {
               return _coordinator;
            }
        }
    }
}