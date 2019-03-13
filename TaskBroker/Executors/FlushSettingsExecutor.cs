using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;
using TaskBroker.SSSB.Results;
using TaskBroker.SSSB.Scheduler;

namespace TaskBroker.SSSB.Executors
{
    public class FlushSettingsExecutor : BaseExecutor
    {
        private readonly IScheduleManager _scheduleManager;
        private SettingsType _settingsType;
        private int _settingsID;

        public FlushSettingsExecutor(ExecutorArgs args, IScheduleManager scheduleManager) :
            base(args)
        {
            _scheduleManager = scheduleManager;
        }

        public override bool IsAsyncProcessing
        {
            get
            {
                return true;
            }
        }

        protected override Task BeforeExecuteTask(CancellationToken token)
        {
            _settingsType = (SettingsType)Enum.Parse(typeof(SettingsType), this.Parameters["settingsType"]);
            _settingsID = int.Parse(this.Parameters["settingsID"]);
            return Task.CompletedTask;
        }

        protected override async Task<HandleMessageResult> DoExecuteTask(CancellationToken token)
        {
            OnDemandTaskManager.FlushTaskInfos();

            switch (_settingsType)
            {
                case SettingsType.None:
                    break;
                case SettingsType.OnDemandTaskSettings:
                    if (_settingsID < 0)
                        SettingsService.FlushStaticSettings();
                    else
                        SettingsService.FlushStaticSettings(_settingsID);
                    break;
                case SettingsType.Shedule:
                    if (_settingsID < 0)
                    {
                        _scheduleManager.UnLoadSchedules();
                        await _scheduleManager.LoadSchedules();
                    }
                    else
                    {
                        await _scheduleManager.ReloadSchedule(_settingsID);
                    }
                    break;
                default:
                    Logger.LogError(string.Format("UnKnown settingsType: {0}", _settingsType));
                    break;
            }

            return this.Noop();
        }
    }
}
