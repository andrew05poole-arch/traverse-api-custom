#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business.BankRec;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.BankRec.Controllers
{
    public class ApiBrDepositHeaderController : ApiBrTransactionBaseController
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bank/{bankid}/deposit/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Get(string bankId = null, string transId = null)
        {
            return Ok(await Load(bankId, transId, TransactionType.Deposit));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/deposit/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string bankId = null, string transId = null)
        {
            return Ok(await ProcessEditRequest(false, body, bankId, transId, TransactionType.Deposit));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/deposit/{transid?}", typeof(JournalHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string bankId = null, string transId = null)
        {
            return Ok(await ProcessEditRequest(true, body, bankId, transId, TransactionType.Deposit));
        }

        [ApiRoute(FunctionID, 2f, "bank/{bankid}/deposit/{transid}", typeof(JournalHeader))]
        public async Task Delete(string bankId, string transId)
        {
            await MarkToDelete(bankId, transId, TransactionType.Deposit);
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
        public const string FunctionID = "DFD44A2F-0EBB-4A5B-9D5C-49B12F8E143D";
        #endregion Properties
    }
}
