using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.SSSB.Results
{
    public class DeferMessageResult : HandleMessageResult
    {
        private readonly IServiceBrokerHelper _serviceBrokerHelper;
        private readonly string _fromService;
        private readonly TimeSpan _lifeTime;
        private readonly DateTime _activationTime;
        private readonly int _attemptNumber;

        public class Args
        {
            public string FromService { get; set; }
            public DateTime ActivationTime { get; set; }
            public TimeSpan? LifeTime { get; set; }
            public int AttemptNumber { get; set; }
        }

        public DeferMessageResult(IServiceBrokerHelper serviceBrokerHelper, Args args)
        {
            _serviceBrokerHelper = serviceBrokerHelper;
            _fromService = args.FromService ?? throw new ArgumentNullException(nameof(args.FromService));
            _lifeTime = args.LifeTime ?? TimeSpan.FromHours(12);
            _activationTime = args.ActivationTime;
            _attemptNumber = args.AttemptNumber;
        }

        public override Task Execute(SqlConnection dbconnection, SSSBMessage message, CancellationToken token)
        {
            return _serviceBrokerHelper.SendPendingMessage(dbconnection, message, _fromService, _lifeTime, false, _activationTime, null, _attemptNumber);
        }
    }
}
