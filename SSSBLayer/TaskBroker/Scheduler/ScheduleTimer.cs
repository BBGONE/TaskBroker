using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace TaskBroker.SSSB.Scheduler
{
    public class ScheduleTimer : BaseSheduleTimer
    {
        private readonly Timer _timer;
        private readonly double _interval;
        private bool _isFirstTime;
        private readonly Stopwatch _swatch;
        private readonly ILogger<ScheduleTimer> _logger;
      

        public ScheduleTimer(IScheduleManager scheduleManager, int taskID, int interval) 
            : base(scheduleManager, taskID)
        {
            this._logger = scheduleManager.RootServices.GetRequiredService<ILogger<ScheduleTimer>>();
            this._interval = interval;
            this._isFirstTime = true;

            lock (Timers)
            {
                if (Timers.ContainsKey(this.TaskID))
                    throw new Exception("Попытка добавить второй таймер для той же задачи");
                Timers.Add(this.TaskID, this);
            }
            
            // first time delay is 10 seconds
            this._timer = new Timer(10000);
            this._timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
            this._swatch = new Stopwatch();
        }

        async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await this.onTimerElapsed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
        }

        protected override async Task SendEventMessage()
        {
            await base.SendEventMessage();
            this._swatch.Restart();
        }

        public override void Start()
        {
            if (this.Enabled)
                return;

            if (this._swatch.IsRunning)
            {
                this._swatch.Stop();
                this.LazyWaitTime = this._swatch.Elapsed;
            }
            else
            {
                this.LazyWaitTime = TimeSpan.FromSeconds(0);
            }

            this._swatch.Reset();

            // если первый вызов таймера 
            // то он срабатывает через 10 секунд
            if (this._isFirstTime)
            {
                this._isFirstTime = false;
            }
            else
            {
                //все последующие запуски таймера учитывают продолжительность выполнения задачи (this.LazyWaitTime.TotalMilliseconds)
                double timerInterval = this.IntervalMilliSeconds - this.LazyWaitTime.TotalMilliseconds;
                this._timer.Interval = timerInterval < 50 ? 50 : timerInterval; //самый меньший интервал 50 миллисекунд
            }

            this.Enabled = true;
        }

        public override void Stop()
        {
            this.Enabled = false;
        }

        protected double IntervalMilliSeconds
        {
            get { return _interval * 1000; }
        }

        public override bool Enabled
        {
            get
            {
                return this._timer.Enabled;
            }
            protected set
            {
                this._timer.Enabled = value;
            }
        }

        protected override void OnDispose()
        {
            lock (Timers)
            {
                Timers.Remove(this.TaskID);
            }
            this._swatch.Reset();
            this._timer.Close();
            this._timer.Elapsed -= new ElapsedEventHandler(_timer_Elapsed);
            base.OnDispose();
        }

    }
}
