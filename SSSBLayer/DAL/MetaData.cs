using System;
using System.Collections.Generic;

namespace TaskBroker.SSSB.EF
{
    public partial class MetaData
    {
        public int MetaDataId { get; set; }
        public Guid Context { get; set; }
        public bool IsContextConversationHandle { get; set; }
        public int RequestCount { get; set; }
        public int RequestCompleted { get; set; }
        public string Error { get; set; }
        public string Result { get; set; }
        public DateTime CreateDate { get; set; }
        public bool? IsCanceled { get; set; }
        public byte[] RowTimeStamp { get; set; }
    }
}
