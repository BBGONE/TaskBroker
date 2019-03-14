using System;
using TaskBroker.SSSB.EF;

namespace TaskBroker.SSSB.Core
{
    public interface IBaseManager
    {
        IServiceProvider Services { get; }
        SSSBDbContext SSSBDb { get; }

        void Dispose();
    }
}