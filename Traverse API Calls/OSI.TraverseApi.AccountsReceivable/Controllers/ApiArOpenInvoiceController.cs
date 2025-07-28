#region Using Directives
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsReceivable.Controllers
{
    public class ApiArOpenInvoiceController : ApiControllerBase 
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "openinvoice", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/customer/{customerid}", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/customer/{customerid}/invoice/{invoicenum}", typeof(OpenInvoice))]
        public async Task<IHttpActionResult> GetOpen(string customerId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(customerId, invoiceNum, OpenInvoice.OIStatus.Released | OpenInvoice.OIStatus.Hold));
        }

        [ApiRoute(FunctionID, 2f, "openinvoice/paid", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/paid/customer/{customerid}", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/paid/customer/{customerid}/invoice/{invoicenum}", typeof(OpenInvoice))]
        public async Task<IHttpActionResult> GetPaid(string customerId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(customerId, invoiceNum, OpenInvoice.OIStatus.Paid));
        }

        [ApiRoute(FunctionID, 2f, "openinvoice/all/customer/{customerid}", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/all/customer/{customerid}/invoice/{invoicenum}", typeof(OpenInvoice))]
        public async Task<IHttpActionResult> GetAll(string customerId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(customerId, invoiceNum, OpenInvoice.OIStatus.Released | OpenInvoice.OIStatus.Hold | OpenInvoice.OIStatus.Paid));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<OpenInvoice>> Load(string custId, string invcNum, OpenInvoice.OIStatus status)
        {
            var list = SummaryOpenInvoiceProvider.LoadInvoices(this.CompId, custId, invcNum, status);
            await this.FilterEntityListAsync(list);
            return list;
        }
        #endregion Helper Methods

        #region Fields
        public const string FunctionID = "4285E088-483A-4134-8802-4C77D0CEB599";
        #endregion Fields
    }
}
