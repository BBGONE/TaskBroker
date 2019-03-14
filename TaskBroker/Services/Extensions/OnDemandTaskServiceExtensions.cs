using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskBroker.Services;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.EF;
using TaskBroker.SSSB.Services;

namespace TaskBroker.SSSB
{
    public static class OnDemandTaskServiceExtensions
    {
        public static void AddOnDemandTaskService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<SSSBDbContext>((dbOptions) => {
                string connectionString = configuration.GetConnectionString("DBConnectionStringSSSB");
                dbOptions.UseSqlServer(connectionString, (sqlOptions) => {
                    // sqlOptions.UseRowNumberForPaging();
                });
            });
            services.AddSSSBService();
            services.TryAddSingleton<OnDemandTaskSSSBService>();
            services.TryAddScoped<IOnDemandTaskManager, OnDemandTaskManager>();
            services.TryAddScoped<ISettingsService, SettingsService>();
            services.AddHostedService<OnDemandTaskService>();
        }
    }
}
