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
        private readonly Task<HandleMessageResult> _completion;
        private readonly TaskCompletionSource<HandleMessageResult> _tcs;
        private readonly IServiceScope _serviceScope;
        private readonly IServiceProvider _services;
        private int _taskID;

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

        public ServiceMessageEventArgs(ServiceMessageEventArgs parentArgs, SSSBMessage message)
        {
            _message = message ?? parentArgs.Message;
            _service = parentArgs._service;
            _token = parentArgs._token;
            _taskID = parentArgs._taskID;
            _serviceScope = parentArgs._serviceScope;
            _tcs = parentArgs._tcs;
            _completion = parentArgs._completion;
            _services = parentArgs._services;
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

        public TaskCompletionSource<HandleMessageResult> TaskCompletionSource => _tcs;
    
        public SSSBMessage Message => _message;
    
        public ISSSBService SSSBService => _service;
     
        public CancellationToken Token => _token;
      
        public Task<HandleMessageResult> Completion => _completion;
       
        public IServiceProvider Services => _services;
        
        public IServiceScope ServiceScope => _serviceScope;

        public void Dispose()
        {
            ServiceScope.Dispose();
        }
    }
}
