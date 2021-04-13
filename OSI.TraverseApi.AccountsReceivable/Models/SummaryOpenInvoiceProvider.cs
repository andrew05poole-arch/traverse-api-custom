#region Using Directives
using System.Collections.Generic;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.AccountsReceivable
{
    public class SummaryOpenInvoiceProvider
    {
        #region Constructor
        public SummaryOpenInvoiceProvider() { }
        #endregion Constructor

        #region Public Methods
        public static EntityList<OpenInvoice> LoadInvoices(string compId, string custId, string invcNum, OpenInvoice.OIStatus status)
        {
            var provider = new OpenInvoiceProvider();
            SqlFilterBuilder<OpenInvoiceBase.Columns> builder = new SqlFilterBuilder<OpenInvoiceBase.Columns>();

            if (!string.IsNullOrWhiteSpace(custId))
                builder.AppendEquals(OpenInvoiceBase.Columns.CustId, custId);
            if (!string.IsNullOrWhiteSpace(invcNum))
                builder.AppendEquals(OpenInvoiceBase.Columns.InvcNum, invcNum);

            string statusList = string.Empty;
            if ((status & OpenInvoice.OIStatus.Hold) == OpenInvoice.OIStatus.Hold)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)OpenInvoice.OIStatus.Hold);
            if ((status & OpenInvoice.OIStatus.Released) == OpenInvoice.OIStatus.Hold)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)OpenInvoice.OIStatus.Released);
            if ((status & OpenInvoice.OIStatus.Paid) == OpenInvoice.OIStatus.Hold)
                statusList += (string.IsNullOrEmpty(statusList) ? "" : ", ") + string.Format("{0}", (byte)OpenInvoice.OIStatus.Paid);

            if (!string.IsNullOrWhiteSpace(statusList))
                builder.AppendIn(OpenInvoiceBase.Columns.RecType, string.Format("{0}", statusList),true);

            provider.Load(compId, new FilterCriteria(builder.ToString(), ""));

            return provider.Items;
        }

        public static List<SummaryOpenInvoice> Summarize(EntityList<OpenInvoice> invoiceList)
        {
            List<SummaryOpenInvoice> list = new List<SummaryOpenInvoice>();

            foreach (OpenInvoice record in invoiceList)
            {
                SummaryOpenInvoice item = list.Find(i => StringHelper.AreEqual(i.CustId, record.CustId, false)
                                                    && StringHelper.AreEqual(i.InvoiceNum, record.InvcNum, false));
                if (item == null)
                {
                    item = new SummaryOpenInvoice
                    {
                        CustId = record.CustId,
                        InvoiceNum = record.InvcNum
                    };

                    list.Add(item);
                }

                switch (record.RecType)
                {
                    case 1:
                        item.InvoiceList.Add(record);
                        break;
                    case -1:
                        item.CreditMemoList.Add(record);
                        break;
                    case -2:
                        item.PaymentList.Add(record);
                        break;
                    case 4:
                        item.FinanceChargeList.Add(record);
                        break;
                }
            }

            list.ForEach(item => { item.Calculate(); });

            return list;
        }
        #endregion Public Methods

        #region Properties
        protected OpenInvoiceProvider Provider { get; } = new OpenInvoiceProvider();
        #endregion Properties
    }
}
