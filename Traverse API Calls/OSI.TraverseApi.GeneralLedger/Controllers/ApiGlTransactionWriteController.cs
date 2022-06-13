#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Batching;
using TRAVERSE.Business.GeneralLedger;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.GeneralLedger.Controllers
{
    public class ApiGlTransactionWriteController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "year/{fiscalyear:range(0,32767)}/write/transaction/{username?}", typeof(TransactionHeader))]
        [ApiRoute(FunctionID, 2f, "write/transaction/{username?}", typeof(TransactionHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string username = null, short? fiscalYear = null)
        {
            return Ok(await ProcessEditRequest(body, username, fiscalYear));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<TransactionHeader>> Load(string id, string username, short? year)
        {
            var builder = new SqlFilterBuilder<TransactionBase.Columns>();
            if (year.GetValueOrDefault() == 0)
                builder.AppendEquals(TransactionBase.Columns.FiscalYear, Account.AccountInfo(this.CompId).CurrentFiscalYear.ToString());
            else
                builder.AppendEquals(TransactionBase.Columns.FiscalYear, year.ToString());

            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(TransactionBase.Columns.BatchId, id);

            if (!string.IsNullOrEmpty(username))
                builder.AppendEquals(TransactionBase.Columns.UserId, username);

            var list = this.Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), ""));

            await this.FilterEntityListAsync(list);

            return list;
        }

        protected virtual async Task<EntityList<TransactionHeader>> ProcessEditRequest(dynamic body, string username, short? year)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            var entityList = new EntityList<TransactionHeader>();
            if (list.Length == 0)
            {
                entityList.AddRange(await Load(null, username, year));
            }
            else
            {
                foreach (dynamic item in list)
                {
                    entityList = await this.ProcessBodyItem(item, username, year);
                }
            }

            WriteToJournal(entityList);
            UpdateBatch(entityList);
            CreateActivity();

            return entityList;
        }

        protected virtual async Task<EntityList<TransactionHeader>> ProcessBodyItem(dynamic bodyItem, string username, short? year)
        {
            string code = ApiUserSkipped.IsApiUserSkipped(bodyItem.BatchId) ? null : bodyItem.BatchId;
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidValueException("Batch ID is required for the write request.");

            var list = await this.Load(code, username, year);

            if (list == null)
                throw new InvalidValueException(string.Format("No transactions for batch '{0}' could be found.", code));

            return list;
        }

        protected virtual void WriteToJournal(EntityList<TransactionHeader> list)
        {
            if (list.Count == 0)
                throw new NothingToProcessException("No transactions found to write.");

            ProcessEngine.Comments = string.Format("API Write {0}", this.GetClientIpAddress());
            ProcessEngine.AllowUnbalanced = false;
            ProcessEngine.TransactionHeaderList.AddRange(list);
            if (!ProcessEngine.CheckClosedPeriod())
            {
                List<string> closedPds = new List<string>();
                foreach (Transaction trans in ProcessEngine.ClosedPeriodTransactionList)
                {
                    string period = string.Format("{0}/{1}", trans.FiscalPeriod, trans.FiscalYear);
                    if (!closedPds.Contains(period))
                        closedPds.Add(period);
                }

                if (closedPds.Count > 0)
                    throw new PeriodClosedException(string.Format("The following periods are closed: {0}", string.Join(" | ", closedPds)));
            }
            
            ProcessEngine.Execute(null);
        }

        protected virtual void UpdateBatch(EntityList<TransactionHeader> list)
        {
            List<string> batchList = new List<string>();
            foreach (TransactionHeader header in list)
            {
                if (!batchList.Contains(header.BatchId.ToLower()))
                    batchList.Add(header.BatchId.ToLower());
            }

            foreach (string batchId in batchList)
                UpdateBatch(batchId);
        }

        protected virtual void UpdateBatch(string batchId)
        {
            List<IBatch> list = new List<IBatch>();
            GLTransactionBatch transactionBatch = EntityProvider.GetEntity<GLTransactionBatch, GLTransactionBatchProvider>(new string[]
            {
                Utility.FunctionIdTransaction,
                batchId
            }, this.CompId, null);

            if (transactionBatch.CanDelete())
            {
                list.Add(transactionBatch);
            }
            Batch.RemoveBatchList(list, Utility.FunctionIdTransaction, Utility.GlobalBatchId, this.CompId, null);
            ListFactory.RefreshList(typeof(GLTransactionBatchProvider), this.CompId, null);
        }

        protected virtual void CreateActivity()
        {
            ProcessEngine.FunctionId = new Guid("a269bf69-5b38-4a81-bbc0-180817e83c9f");
            ProcessEngine.Description = "GL Transactions";
            ProcessEngine.CreateActivity();
        }
        #endregion Helper Methods

        #region Properties
        protected ApiTransactionHeaderProvider Provider { get; } = new ApiTransactionHeaderProvider();

        protected virtual TransactionPost ProcessEngine 
        {
            get => _processEngine != null ? _processEngine : (_processEngine = new TransactionPost(this.CompId, ProcessBase.GenerateProcessId()));
        }
        #endregion Properties

        #region Fields
        private TransactionPost _processEngine;

        private const string FunctionID = "54894301-B3D8-4E1D-9D50-9117CBE2E447";
        #endregion Fields
    }
}
