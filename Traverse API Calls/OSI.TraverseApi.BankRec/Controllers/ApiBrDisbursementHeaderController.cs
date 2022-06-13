#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.BankRec;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.BankRec.Controllers
{
    public class ApiBrDisbursementHeaderController : ApiBrTransactionBaseController
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bank/{bankid}/disbursement/{transid?}", typeof(JournalHeader))]
        public async  Task<IHttpActionResult> Get(string bankId = null, string transId = null)
        {
            return Ok(await Load(bankId, transId, TransactionType.Disbursement));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/disbursement/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string bankId = null, string transId = null)
        {
            return Ok(await ProcessEditRequest(false, body, bankId, transId, TransactionType.Disbursement));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/disbursement/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string bankId = null, string transId = null)
        {
            return Ok(await ProcessEditRequest(true, body, bankId, transId, TransactionType.Disbursement));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/disbursement/{transid}", typeof(JournalHeader))]
        public async Task Delete(string bankId, string transId)
        {
            await MarkToDelete(bankId, transId, TransactionType.Disbursement);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            base.AddPropertyDelegates();
        }
        #endregion Overrides
        #endregion Helper Methods

        #region Properties
        public const string FunctionID = "78BD0933-A05B-4437-856C-E6AD7FD84213";
        #endregion Properties
    }
}
