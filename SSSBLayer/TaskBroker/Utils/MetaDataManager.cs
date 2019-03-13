using TaskBroker.SSSB.EF;
using Microsoft.EntityFrameworkCore;
using Shared.Database;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskBroker.SSSB.Core;

namespace TaskBroker.SSSB.Utils
{
    public class MetaDataManager : BaseManager, IMetaDataManager
    {
        private int _metaDataID;

        public MetaDataManager(int MetaDataID, IServiceProvider services) :
            base(services)
        {
            this._metaDataID = MetaDataID;
        }

        public Task<MetaData> GetMetaData(CancellationToken token = default(CancellationToken))
        {
            return this.SSSBDb.MetaData.AsNoTracking().Where(md => md.MetaDataId == this.MetaDataID).SingleAsync(token);
        }

        public CompletionResult IsAllTasksCompleted(MetaData metaData)
        {
            if (metaData.Error != null)
            {
                return CompletionResult.Error;
            }
            bool isCancelled = metaData.IsCanceled == true;
            if (isCancelled)
            {
                return CompletionResult.Cancelled;
            }
            return (metaData.RequestCount == metaData.RequestCompleted) ? CompletionResult.Completed : CompletionResult.None;
        }

        private async Task<CompletionResult> _SetCompleted(string error = null, string result = null, bool? isCancelled = null)
        {
            var idParam = new SqlParameter("@MetaDataID", System.Data.SqlDbType.Int);
            idParam.Value = _metaDataID;
            var resultParam = new SqlParameter("@Result", System.Data.SqlDbType.NVarChar, 255);
            resultParam.Value = NullableHelper.DBNullConvertFrom(result);
            var cancelledParam = new SqlParameter("@isCanceled", System.Data.SqlDbType.Bit);
            cancelledParam.Value = NullableHelper.DBNullConvertFrom(isCancelled);
            var errorParam = new SqlParameter("@ErrorMessage", System.Data.SqlDbType.NVarChar, 4000);
            errorParam.Value = NullableHelper.DBNullConvertFrom(error);

            if (error != null)
            {
                return CompletionResult.Error;
            }

            if (isCancelled == true)
            {
                return CompletionResult.Cancelled;
            }

            await this.SSSBDb.Database.ExecuteSqlCommandAsync("EXEC [PPS].[sp_SetCompleted] @MetaDataID, @Result, @isCanceled,  @ErrorMessage", new object[] { idParam, resultParam, cancelledParam, errorParam });
            MetaData metaData = await GetMetaData(CancellationToken.None);
            return IsAllTasksCompleted(metaData);
        }

        public Task<CompletionResult> SetCancelled()
        {
            return _SetCompleted(null, null, true);
        }

        public Task<CompletionResult> SetCompleted()
        {
            return _SetCompleted();
        }
        
        public Task<CompletionResult> SetCompletedWithError(string error)
        {
            return _SetCompleted(error);
        }

        public Task<CompletionResult> SetCompletedWithResult(string result)
        {
            return _SetCompleted(null, result);
        }

        public int MetaDataID
        {
            get { return _metaDataID; }
        }
    }
}
