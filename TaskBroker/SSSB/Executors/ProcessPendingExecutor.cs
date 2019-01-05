﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TaskCoordinator.Database;
using TaskCoordinator.SSSB;

namespace TaskBroker.SSSB.Executors
{
    public class ProcessPendingExecutor : BaseExecutor
    {
        private readonly IServiceBrokerHelper _issbHelper;
        private readonly bool _processAll;
        private readonly string _objectID;

        public ProcessPendingExecutor(ExecutorArgs args, IServiceBrokerHelper isssbHelper) :
            base(args)
        {
            _issbHelper = isssbHelper;
            _processAll = false;
            _objectID = null;
        }

        public override bool IsLongRunning
        {
            get
            {
                return false;
            }
        }

        protected override void BeforeExecuteTask()
        {
           
        }

        protected override async Task<HandleMessageResult> DoExecuteTask(CancellationToken token)
        {
            var connectionManager = this.Services.GetRequiredService<IConnectionManager>();
            using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = await connectionManager.CreateSSSBConnectionAsync(CancellationToken.None))
            {
                await _issbHelper.ProcessPendingMessages(connection, _processAll, _objectID);
            }

            return EndDialog();
        }
    }
}