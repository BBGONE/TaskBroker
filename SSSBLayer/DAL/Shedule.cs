using System;
using System.Collections.Generic;

namespace TaskBroker.SSSB.EF
{
    public partial class Shedule
    {
        public Shedule()
        {
            OnDemandTask = new HashSet<OnDemandTask>();
        }

        public int SheduleId { get; set; }
        public string Name { get; set; }
        public int Interval { get; set; }
        public bool? Active { get; set; }
        public DateTime CreateDate { get; set; }
        public byte[] RowTimeStamp { get; set; }

        public virtual ICollection<OnDemandTask> OnDemandTask { get; set; }
    }
}
