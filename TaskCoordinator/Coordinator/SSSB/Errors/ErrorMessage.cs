﻿using System;

namespace TaskCoordinator.SSSB
{
    public class ErrorMessage
    {
        public Guid MessageID;
        public int ErrorCount;
        public DateTime LastAccess;
        public Exception FirstError;
    }
}
