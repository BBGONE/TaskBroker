using TaskBroker.SSSB.EF;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace TaskBroker.SSSB.Core
{
    public class BaseManager : IDisposable, IBaseManager
    {
        private readonly Lazy<SSSBDbContext> _db;
        private readonly IServiceProvider _services;

        public BaseManager(IServiceProvider services)
        {
            this._services = services;
            this._db = new Lazy<SSSBDbContext>(() => { return this._services.GetRequiredService<SSSBDbContext>(); }, true);
        }

        public SSSBDbContext SSSBDb
        {
            get
            {
                return this._db.Value;
            }
        }

        public IServiceProvider Services => _services;

        protected virtual void OnDispose()
        {
            // NOOP
        }

        #region IDisposable Members

        public void Dispose()
        {
           this.OnDispose();
        }

        #endregion
    }
}
