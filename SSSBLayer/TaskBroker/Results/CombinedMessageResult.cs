using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.Results
{
    public class CombinedMessageResult : HandleMessageResult
    {
        private readonly IEnumerable<HandleMessageResult> _resultHandlers;

        public CombinedMessageResult(params HandleMessageResult[] resultHandlers)
        {
            _resultHandlers = resultHandlers ?? throw new ArgumentNullException(nameof(resultHandlers));
        }

        public override async Task Execute(SqlConnection dbconnection, SSSBMessage message, CancellationToken token)
        {
            foreach (var handler in _resultHandlers) {
                await handler.Execute(dbconnection, message, token);
            }
        }
    }
}
