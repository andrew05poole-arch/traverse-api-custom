#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business.BankRec;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.BankRec.Controllers
{
    public class ApiBrDisbursementDetailController : ApiBrTransactionDetailBaseController
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bank/{bankid}/disbursement/{transid}/lineentry/{entrynumber?}", typeof(JournalDetail))]
        public async Task<IHttpActionResult> Get(string bankId = null, string transId = null, int? entryNumber = null)
        {
            return Ok(await Load(bankId, transId, entryNumber, TransactionType.Disbursement));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/disbursement/{transid}/lineentry/{entrynumber?}", typeof(JournalDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string bankId = null, string transId = null, int? entryNumber = null)
        {
            return Ok(await ProcessEditRequest(false, body, bankId, transId, entryNumber, TransactionType.Disbursement));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            base.AddPropertyDelegates();
        }
        #endregion Helper Methods

        #region Properties
        public const string FunctionID = "6B501595-0464-4903-A032-96B296E0E06E";
        #endregion Properties
    }
}
