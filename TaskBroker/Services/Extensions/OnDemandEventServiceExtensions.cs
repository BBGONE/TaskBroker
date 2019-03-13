using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskBroker.Services;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.EF;
using TaskBroker.SSSB.Scheduler;
using TaskBroker.SSSB.Services;

namespace TaskBroker.SSSB
{
    public static class OnDemandEventServiceExtensions
    {
        public static void AddOnDemandEventService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<SSSBDbContext>((dbOptions) => {
                string connectionString = configuration.GetConnectionString("DBConnectionStringSSSB");
                dbOptions.UseSqlServer(connectionString, (sqlOptions) => {
                    // sqlOptions.UseRowNumberForPaging();
                });
            });
            services.AddSSSBService();
            services.TryAddSingleton<IScheduleManager, ScheduleManager>();
            services.TryAddSingleton<OnDemandEventSSSBService>();
            services.AddHostedService<OnDemandEventService>();
        }
    }
}
