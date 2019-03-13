using System;
using System.Collections.Generic;

namespace TaskBroker.SSSB.EF
{
    public partial class OnDemandTask
    {
        public int OnDemandTaskId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool? Active { get; set; }
        public short ExecutorId { get; set; }
        public int? SheduleId { get; set; }
        public int? SettingId { get; set; }
        public string SssbserviceName { get; set; }
        public DateTime CreateDate { get; set; }
        public byte[] RowTimeStamp { get; set; }

        public virtual Executor Executor { get; set; }
        public virtual Setting Setting { get; set; }
        public virtual Shedule Shedule { get; set; }
    }
}
