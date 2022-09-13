#region Using Directives
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsPayable;
using TRAVERSE.Core;
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsPayable.Models
{
    public class SummaryOpenInvoiceProvider
    {
        #region Constructor
        public SummaryOpenInvoiceProvider() { }
        #endregion Constructor

        #region Public Methods
        public static EntityList<OpenInvoice> LoadInvoices(string compId, string vendorId, string invcNum, ApOpenInvoice.OIStatus status)
        {
            var provider = new OpenInvoiceProvider();
            SqlFilterBuilder<OpenInvoiceBase.Columns> builder = new SqlFilterBuilder<OpenInvoiceBase.Columns>();

            if (!string.IsNullOrWhiteSpace(vendorId))
                builder.AppendEquals(OpenInvoiceBase.Columns.VendorId, vendorId);
            if (!string.IsNullOrWhiteSpace(invcNum))
                builder.AppendEquals(OpenInvoiceBase.Columns.InvoiceNum, invcNum);

            string statusList = string.Empty;
            if ((status & ApOpenInvoice.OIStatus.Hold) == ApOpenInvoice.OIStatus.Hold)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)ApOpenInvoice.OIStatus.Hold);
            if ((status & ApOpenInvoice.OIStatus.Released) == ApOpenInvoice.OIStatus.Released)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)ApOpenInvoice.OIStatus.Released);
            if ((status & ApOpenInvoice.OIStatus.Paid) == ApOpenInvoice.OIStatus.Paid)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)ApOpenInvoice.OIStatus.Paid);
            if ((status & ApOpenInvoice.OIStatus.Temp) == ApOpenInvoice.OIStatus.Temp)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)ApOpenInvoice.OIStatus.Temp);
            if ((status & ApOpenInvoice.OIStatus.Prepaid) == ApOpenInvoice.OIStatus.Prepaid)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)ApOpenInvoice.OIStatus.Prepaid);
            if (!string.IsNullOrWhiteSpace(statusList))
                builder.AppendIn(OpenInvoiceBase.Columns.Status, string.Format("{0}", statusList),true);

            provider.Load(compId, new FilterCriteria(builder.ToString(), string.Empty));

            return provider.Items;
        }    
        #endregion Public Methods

        #region Properties
        protected OpenInvoiceProvider Provider { get; } = new OpenInvoiceProvider();
        #endregion Properties
    }
}
