using System;

namespace TaskBroker.SSSB.Errors
{
    public class ErrorMessage
    {
        public Guid MessageID;
        public int ErrorCount;
        public DateTime LastAccess;
        public Exception FirstError;
    }
}
