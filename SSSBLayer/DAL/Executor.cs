using System;
using System.Collections.Generic;

namespace TaskBroker.SSSB.EF
{
    public partial class Executor
    {
        public Executor()
        {
            OnDemandTask = new HashSet<OnDemandTask>();
        }

        public short ExecutorId { get; set; }
        public string Description { get; set; }
        public string FullTypeName { get; set; }
        public bool? Active { get; set; }
        public bool IsMessageDecoder { get; set; }
        public bool IsOnDemand { get; set; }
        public string ExecutorSettingsSchema { get; set; }
        public DateTime CreateDate { get; set; }
        public byte[] RowTimeStamp { get; set; }

        public virtual ICollection<OnDemandTask> OnDemandTask { get; set; }
    }
}
