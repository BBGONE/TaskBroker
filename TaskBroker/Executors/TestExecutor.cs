﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Coordinator.SSSB;

namespace TaskBroker.SSSB.Executors
{
    public class TestExecutor : BaseExecutor
    {
        private string _batchId;
        private string _category;
        private string _clientContextID;

        public TestExecutor(ExecutorArgs args):
            base(args)
        {
        }

        protected override Task BeforeExecuteTask(CancellationToken token)
        {
            _category = this.Parameters["Category"];
            _batchId = this.Parameters["BatchID"];
            _clientContextID = this.Parameters["ClientContext"];
            return Task.CompletedTask;
        }

        protected override async Task<HandleMessageResult> DoExecuteTask(CancellationToken token)
        {
            this.Debug(string.Format("Executing SSSB Task: {0} Batch: {1} ClientContextID {2}", this.TaskInfo.OnDemandTaskID, _batchId, _clientContextID));
            if (this.AttemptNumber == 0)
            {
                this.Debug(string.Format("*** Defer SSSB Task: {0} Batch: {1} ClientContextID {2}", this.TaskInfo.OnDemandTaskID, _batchId, _clientContextID ));
                Guid initiatorConversationGroup = Guid.Parse(_clientContextID);
                return this.Defer("PPS_OnDemandTaskService", DateTime.Now.AddSeconds(10), this.AttemptNumber + 1);
            }
            else
            {
                await Task.Delay(3000);
                this.Debug(string.Format("Executed  SSSB Task: {0} Batch: {1} ClientContextID {2}", this.TaskInfo.OnDemandTaskID, _batchId, _clientContextID));
                return this.EndDialog();
            }
        }

        protected override Task AfterExecuteTask(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
