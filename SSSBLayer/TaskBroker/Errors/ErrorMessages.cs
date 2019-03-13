using Microsoft.Extensions.Logging;
using Shared.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TaskBroker.SSSB.Errors
{
    /// <summary>
    /// В случае ошибки обработки сообщения
    /// id  сообщения добавляется в словарь
    /// это нужно для проверки-  следует ли снова обрабатывать это сообщение?
    /// </summary>
    public class ErrorMessages : Dictionary<Guid, ErrorMessage>, IErrorMessages
    {
        readonly Timer _cleanUpTimer;
        readonly ILogger _logger;

        protected static ReaderWriterLockSlim _dictLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public ErrorMessages(ILogger<ErrorMessages> logger)
            : base()
        {
            this._logger = logger;
            // once in hour
            this._cleanUpTimer = new Timer(TimerCallback, null, TimeSpan.FromMinutes(245), TimeSpan.FromMinutes(60));
        }

        private void TimerCallback(object state)
        {
            this.ClearOldErrors(TimeSpan.FromMinutes(240));
        }

        public int GetErrorCount(Guid messageID)
        {
            ErrorMessage res = GetError(messageID);
            if (res != null)
                return res.ErrorCount;
            else
                return 0;
        }

        public ErrorMessage GetError(Guid messageID)
        {
            _dictLock.EnterReadLock();
            try
            {
                ErrorMessage res;
                if (this.TryGetValue(messageID, out res))
                {
                    res.LastAccess = DateTime.Now;
                    return res;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
            finally
            {
                _dictLock.ExitReadLock();
            }
            return null;
        }

        public int AddError(Guid messageID, Exception err)
        {
            ErrorMessage res;
            _dictLock.EnterUpgradeableReadLock();
            try
            {
                if (this.TryGetValue(messageID, out res))
                {
                    Interlocked.Increment(ref res.ErrorCount);
                    res.LastAccess = DateTime.Now;
                }
                else
                {
                    res = new ErrorMessage();
                    res.MessageID = messageID;
                    res.ErrorCount = 1;
                    res.FirstError = err;
                    res.LastAccess = DateTime.Now;

                    _dictLock.EnterWriteLock();
                    try
                    {
                        this.Add(messageID, res);
                    }
                    finally
                    {
                        _dictLock.ExitWriteLock();
                    }
                }

                return res.ErrorCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
            finally
            {
                _dictLock.ExitUpgradeableReadLock();
            }
            return 0;
        }

        public bool RemoveError(Guid messageID)
        {
            _dictLock.EnterUpgradeableReadLock();
            try
            {
                if (!this.ContainsKey(messageID))
                    return false;
                else
                {
                    _dictLock.EnterWriteLock();
                    try
                    {
                        this.Remove(messageID);
                    }
                    finally
                    {
                        _dictLock.ExitWriteLock();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
            }
            finally
            {
                _dictLock.ExitUpgradeableReadLock();
            }
            return false;
        }

        public void ClearErrors()
        {
            _dictLock.EnterWriteLock();
            try
            {
                this.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ErrorHelper.GetFullMessage(ex));
                return;
            }
            finally
            {
                _dictLock.ExitWriteLock();
            }
        }

        public void ClearOldErrors(TimeSpan maxAge)
        {
            ErrorMessage[] errors = new ErrorMessage[0];
            _dictLock.EnterReadLock();
            try
            {
                errors = this.Values.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ErrorHelper.GetFullMessage(ex));
                return;
            }
            finally
            {
                _dictLock.ExitReadLock();
            }


            DateTime now = DateTime.Now;
            for (int i = 0; i < errors.Length; ++i)
            {
                ErrorMessage err = errors[i];
                TimeSpan errAge = now - err.LastAccess;
                if (errAge.TotalMilliseconds > maxAge.TotalMilliseconds)
                {
                    this.RemoveError(err.MessageID);
                }
            }
        }

        public int ErrorMessageCount
        {
            get
            {
                _dictLock.EnterReadLock();
                try
                {
                    return this.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ErrorHelper.GetFullMessage(ex));
                }
                finally
                {
                    _dictLock.ExitReadLock();
                }
                return 0;
            }
        }
    }
}
