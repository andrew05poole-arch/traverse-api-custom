#region Using Directives
using TRAVERSE.Web.API.AccountsPayable.Models;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsPayable;
using TRAVERSE.Web.API;
#endregion

namespace TRAVERSE.Web.API.AccountsPayable.Controllers
{
    public class ApiApOpenInvoiceController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "openinvoice", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/vendor/{vendorid}", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/vendor/{vendorid}/invoice/{invoicenum}", typeof(OpenInvoice))]
        public async Task<IHttpActionResult> GetOpen(string vendorId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(vendorId, invoiceNum, ApOpenInvoice.OIStatus.Released | ApOpenInvoice.OIStatus.Hold | ApOpenInvoice.OIStatus.Temp | ApOpenInvoice.OIStatus.Prepaid));
        }

        [ApiRoute(FunctionID, 2f, "openinvoice/paid", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/paid/vendor/{vendorid}", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/paid/vendor/{vendorid}/invoice/{invoicenum}", typeof(OpenInvoice))]
        public async Task<IHttpActionResult> GetPaid(string vendorId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(vendorId, invoiceNum, ApOpenInvoice.OIStatus.Paid));
        }

        [ApiRoute(FunctionID, 2f, "openinvoice/all/vendor/{vendorid}", typeof(OpenInvoice))]
        [ApiRoute(FunctionID, 2f, "openinvoice/all/vendor/{vendorid}/invoice/{invoicenum}", typeof(OpenInvoice))]
        public async Task<IHttpActionResult> GetAll(string vendorId = null, string invoiceNum = null)
        {
            return Ok(await this.Load(vendorId, invoiceNum, ApOpenInvoice.OIStatus.Released | ApOpenInvoice.OIStatus.Hold | ApOpenInvoice.OIStatus.Temp | ApOpenInvoice.OIStatus.Prepaid | ApOpenInvoice.OIStatus.Paid));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<OpenInvoice>> Load(string vendorId, string invcNum, ApOpenInvoice.OIStatus status)
        {
            var list = SummaryOpenInvoiceProvider.LoadInvoices(this.CompId, vendorId, invcNum, status);
            await this.FilterEntityListAsync(list);
            return list;
        }
        #endregion Helper Methods
               
        #region Fields
        public const string FunctionID = "436F98CF-C150-401C-8F83-48AE5FD54A6D";
        #endregion Fields
    }
}
