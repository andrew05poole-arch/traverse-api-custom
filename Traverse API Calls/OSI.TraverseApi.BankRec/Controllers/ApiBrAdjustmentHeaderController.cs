#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business.BankRec;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.BankRec.Controllers
{
    public class ApiBrAdjustmentHeaderController : ApiBrTransactionBaseController
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bank/{bankid}/adjustment/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Get(string bankId = null, string transId = null)
        {
            return Ok(await Load(bankId, transId,TransactionType.Adjustment));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/adjustment/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string bankId = null, string transId = null)
        {
            return Ok(await ProcessEditRequest(false, body,bankId, transId, TransactionType.Adjustment));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/adjustment/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string bankId = null, string transId = null)
        {
            return Ok(await ProcessEditRequest(true, body, bankId, transId, TransactionType.Adjustment));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/adjustment/{transid}", typeof(JournalHeader))]
        public async Task Delete(string bankId, string transId)
        {
           await MarkToDelete(bankId, transId, TransactionType.Adjustment);
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
        public const string FunctionID = "7FA08BCE-0AD5-4C92-A70F-AC77D7253A4C";
        #endregion Properties       
    }
}
