using System;
using System.Collections.Generic;

namespace TaskBroker.SSSB.EF
{
    public partial class Setting
    {
        public Setting()
        {
            OnDemandTask = new HashSet<OnDemandTask>();
        }

        public int SettingId { get; set; }
        public string Description { get; set; }
        public string Settings { get; set; }
        public DateTime CreateDate { get; set; }
        public byte[] RowTimeStamp { get; set; }

        public virtual ICollection<OnDemandTask> OnDemandTask { get; set; }
    }
}
