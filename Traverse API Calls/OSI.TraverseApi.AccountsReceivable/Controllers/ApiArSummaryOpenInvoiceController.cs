#region Using Directives
using OSI.TraverseApi.AccountsReceivable;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.AccountsReceivable.Controllers
{
    public class ApiArSummaryOpenInvoiceController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "summary_invoice/open", typeof(SummaryOpenInvoice))]
        [ApiRoute(FunctionID, 2f, "summary_invoice/open/customer/{customerid}", typeof(SummaryOpenInvoice))]
        [ApiRoute(FunctionID, 2f, "summary_invoice/open/customer/{customerid}/invoice/{invoicenum}", typeof(SummaryOpenInvoice))]
        public async Task<IHttpActionResult> GetOpen(string customerId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(customerId, invoiceNum, OpenInvoice.OIStatus.Released | OpenInvoice.OIStatus.Hold));
        }

        [ApiRoute(FunctionID, 2f, "summary_invoice/paid", typeof(SummaryOpenInvoice))]
        [ApiRoute(FunctionID, 2f, "summary_invoice/paid/customer/{customerid}", typeof(SummaryOpenInvoice))]
        [ApiRoute(FunctionID, 2f, "summary_invoice/paid/customer/{customerid}/invoice/{invoicenum}", typeof(SummaryOpenInvoice))]
        public async Task<IHttpActionResult> GetPaid(string customerId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(customerId, invoiceNum, OpenInvoice.OIStatus.Paid));
        }

        [ApiRoute(FunctionID, 2f, "summary_invoice/all/customer/{customerid}", typeof(SummaryOpenInvoice))]
        [ApiRoute(FunctionID, 2f, "summary_invoice/all/customer/{customerid}/invoice/{invoicenum}", typeof(SummaryOpenInvoice))]
        public async Task<IHttpActionResult> GetAll(string customerId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(customerId, invoiceNum, OpenInvoice.OIStatus.Released | OpenInvoice.OIStatus.Hold | OpenInvoice.OIStatus.Paid));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<List<SummaryOpenInvoice>> Load(string custId, string invcNum, OpenInvoice.OIStatus status)
        {
            var list = SummaryOpenInvoiceProvider.LoadInvoices(this.CompId, custId, invcNum, status);
            await this.FilterEntityListAsync(list, ApiArOpenInvoiceController.FunctionID);

            var summaryList = SummaryOpenInvoiceProvider.Summarize(list);
            await this.FilterEntityListAsync(summaryList);

            return summaryList;
        }
        #endregion Helper Methods

        #region Fields
        public const string FunctionID = "2A22A47A-0365-4420-9692-97186BDA66FD";
        #endregion Fields
    }
}
