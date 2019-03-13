using System;

namespace TaskBroker.SSSB.Errors
{
    public interface IErrorMessages
    {
        int ErrorMessageCount { get; }

        int AddError(Guid messageID, Exception err);
        ErrorMessage GetError(Guid messageID);
        int GetErrorCount(Guid messageID);
        bool RemoveError(Guid messageID);
    }
}