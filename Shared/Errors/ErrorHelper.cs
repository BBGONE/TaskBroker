using System;

namespace Shared.Errors
{
	public static class ErrorHelper
	{
		public static string GetFullMessage(this Exception exception, bool includeStackTrace)
		{
			string result = exception.GetType().Name + ": " + exception.Message;
			while (exception.InnerException != null)
			{
                result = exception.InnerException.GetType().Name + ": " + exception.InnerException.Message + Environment.NewLine + result;
				exception = exception.InnerException;
			}
            if (includeStackTrace)
            {
                if (exception.InnerException != null)
                {
                    result = result + Environment.NewLine + exception.InnerException.StackTrace;
                }
                else
                {
                    result = result + Environment.NewLine + exception.StackTrace;
                }
            }

			return result;
		}

        public static string GetFullMessage(this Exception exception)
        {
            return GetFullMessage(exception, true);
        }
	}
}
