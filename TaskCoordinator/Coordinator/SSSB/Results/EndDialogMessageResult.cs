﻿using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using TaskCoordinator.SSSB.Utils;

namespace TaskCoordinator.SSSB
{
    public class EndDialogMessageResult : HandleMessageResult
    {
        private readonly IServiceBrokerHelper _serviceBrokerHelper;
        private readonly int? _errorCode;
        private readonly string _error;
        private readonly Guid? _conversationHandle;

        public class Args
        {
            public int? errorCode { get; set; }
            public string error { get; set; }
            public Guid? conversationHandle { get; set; }
        }

        public EndDialogMessageResult(IServiceBrokerHelper serviceBrokerHelper, Args args)
        {
            _serviceBrokerHelper = serviceBrokerHelper;
            _error = args.error;
            _errorCode = args.errorCode;
            _conversationHandle = args.conversationHandle;
        }

        public override async Task Execute(SqlConnection dbconnection, SSSBMessage message, CancellationToken token)
        {
            Guid conversationHandle = _conversationHandle.HasValue ? _conversationHandle.Value : message.ConversationHandle;
            if (!string.IsNullOrEmpty(_error))
            {
                await _serviceBrokerHelper.EndConversationWithError(dbconnection, conversationHandle, _errorCode.HasValue? _errorCode.Value: 1, _error);
            }
            else
            {
                await _serviceBrokerHelper.EndConversation(dbconnection, conversationHandle);
            }
        }
    }
}
