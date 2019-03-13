using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Results;

namespace TaskBroker.SSSB.Core
{
    public class ServiceMessageEventArgs : EventArgs, IDisposable
    {
        private readonly ISSSBService _service;
        private readonly SSSBMessage _message;
        private readonly CancellationToken _token;
        private int _taskID;
        private readonly Task<HandleMessageResult> _completion;
        private readonly TaskCompletionSource<HandleMessageResult> _tcs;
        private readonly IServiceScope _serviceScope;
        private readonly IServiceProvider _services;

        public ServiceMessageEventArgs(SSSBMessage message, ISSSBService svc, CancellationToken cancellation, IServiceScope serviceScope)
        {
            _message = message;
            _service = svc;
            _token = cancellation;
            _taskID = -1;
            _serviceScope = serviceScope;
            _tcs = new TaskCompletionSource<HandleMessageResult>();
            _completion = _tcs.Task;
            _services = _serviceScope.ServiceProvider;
        }

        public ServiceMessageEventArgs(ServiceMessageEventArgs args, SSSBMessage newMessage)
        {
            _message = newMessage;
            _service = args._service;
            _token = args._token;
            _taskID = args._taskID;
            _serviceScope = args._serviceScope;
            _tcs = args._tcs;
            _completion = args._completion;
            _services = args._services;
        }

        public TaskCompletionSource<HandleMessageResult> TaskCompletionSource
        {
            get
            {
                return _tcs;
            }
        }

        public SSSBMessage Message
        {
            get { return _message; }
        }

        public ISSSBService SSSBService
        {
            get
            {
                return this._service;
            }
        }

        public int TaskID
        {
            get
            {
                return _taskID;
            }
            set
            {
                _taskID = value;
            }
        }

        public CancellationToken Token
        {
            get
            {
                return _token;
            }
        }

        public Task<HandleMessageResult> Completion
        {
            get { return _completion; }
        }

        public IServiceProvider Services
        {
            get
            {
               return _services;
            }
        }

        public void Dispose()
        {
            _serviceScope.Dispose();
        }
    }
}
