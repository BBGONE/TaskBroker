using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.EF;
using TaskBroker.SSSB.Scheduler;
using TaskBroker.SSSB.Services;
using TaskBroker.SSSB.Utils;

namespace TaskBroker.Services
{
    public static class PubSubServiceExtensions
    {
        public static void AddPubSubService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<SSSBDbContext>((dbOptions) => {
                string connectionString = configuration.GetConnectionString("DBConnectionStringSSSB");
                dbOptions.UseSqlServer(connectionString, (sqlOptions) => {
                    // sqlOptions.UseRowNumberForPaging();
                });
            });
            services.AddSSSBService();
            services.TryAddSingleton<IPubSubHelper, PubSubHelper>();
            services.TryAddSingleton<IScheduleManager, ScheduleManager>();
            services.TryAddSingleton<PubSubSSSBService>((sp)=> {
                var sssb =configuration.GetSection("SSSB");
                Guid conversationGroup = sssb.GetValue<Guid>("InstanceID");
                return ActivatorUtilities.CreateInstance<PubSubSSSBService>(sp, new object[] { conversationGroup });
            });
            services.TryAddSingleton<HeartBeatTimer>((sp) => {
                var sssb = configuration.GetSection("SSSB");
                Guid conversationGroup = sssb.GetValue<Guid>("InstanceID");
                return ActivatorUtilities.CreateInstance<HeartBeatTimer>(sp, new object[] { conversationGroup });
            });
            services.TryAddTransient<OnDemandTaskManager>();
            services.TryAddTransient<ISettingsService, SettingsService>();
            services.AddHostedService<PubSubService>();
        }
    }
}
