#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business.BankRec;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.BankRec.Controllers
{
    public class ApiBrAdjustmentDetailController : ApiBrTransactionDetailBaseController
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bank/{bankid}/adjustment/{transid}/lineentry/{entrynumber?}", typeof(JournalDetail))]
        public async Task<IHttpActionResult> Get(string bankId = null, string transId = null, int? entryNumber = null)
        {
            return Ok(await Load(bankId, transId, entryNumber, TransactionType.Adjustment));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/adjustment/{transid}/lineentry/{entrynumber?}", typeof(JournalDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string bankId = null, string transId = null, int? entryNumber = null)
        {
            return Ok(await ProcessEditRequest(false, body, bankId, transId, entryNumber, TransactionType.Adjustment));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            base.AddPropertyDelegates();
        }
        #endregion Helper Methods

        #region Properties
        public const string FunctionID = "5644D2F4-5B39-4C0A-ADFB-52C0254447E6";
        #endregion Properties
    }
}
