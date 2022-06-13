#region Using Directives
using TRAVERSE.Web.API.AccountsReceivable.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Data;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsReceivable
{
    public class SummaryOpenInvoice
    {
        #region Methods
        public virtual void Calculate() 
        {
            this.InvoiceCount = this.InvoiceList.Count;
            this.FinanceChargeCount = this.FinanceChargeList.Count;
            this.PaymentCount = this.PaymentList.Count;
            this.CreditCount = this.CreditMemoList.Count;
            this.AppliedCount = this.PaymentCount + this.CreditCount;

            foreach (OpenInvoice invoice in this.InvoiceList)
            {
                this.InvoiceTotal += invoice.Amt.GetValueOrDefault();
                this.BalanceDue += invoice.Amt.GetValueOrDefault();

                if (!this.InvcDate.HasValue || this.InvcDate.GetValueOrDefault() > invoice.TransDate.GetValueOrDefault())
                {
                    this.InvcDate = invoice.TransDate;
                }

                if (this.DueDate.GetValueOrDefault() < invoice.NetDueDate.GetValueOrDefault())
                {
                    this.DueDate = invoice.NetDueDate;
                }
            }

            foreach (OpenInvoice invoice in this.FinanceChargeList)
            {
                this.FinanceChargeTotal += invoice.Amt.GetValueOrDefault();
                this.BalanceDue += invoice.Amt.GetValueOrDefault();
            }

            foreach (OpenInvoice invoice in this.CreditMemoList)
            {
                this.CreditTotal += invoice.Amt.GetValueOrDefault();
                this.AppliedTotal += invoice.Amt.GetValueOrDefault();
                this.BalanceDue -= invoice.Amt.GetValueOrDefault();
            }

            foreach (OpenInvoice invoice in this.PaymentList)
            {
                this.PaymentTotal += invoice.Amt.GetValueOrDefault();
                this.AppliedTotal += invoice.Amt.GetValueOrDefault();
                this.BalanceDue -= invoice.Amt.GetValueOrDefault();

                if (this.LastPaymentDate.GetValueOrDefault() < invoice.TransDate.GetValueOrDefault())
                {
                    this.LastPaymentDate = invoice.TransDate;
                }
            }

            if (this.BalanceDue > 0)
                this.InvoiceStatus = "Open";
            else if (this.BalanceDue < 0)
                this.InvoiceStatus = "Overpaid";
            else
                this.InvoiceStatus = "Paid";
        }
        #endregion Public

        #region Properties
        [Bindable(true)]
        public string CustId { get; set; }

        [Bindable(true)]
        public string InvoiceNum { get; set; }

        [Bindable(true)]
        public string InvoiceStatus { get; set; }

        [Bindable(true)]
        public DateTime? InvcDate { get; set; }

        [Bindable(true)]
        public DateTime? DueDate { get; set; }

        [Bindable(true)]
        public DateTime? LastPaymentDate { get; set; }

        [Bindable(true)]
        public int InvoiceCount { get; set; }

        [Bindable(true)]
        public int FinanceChargeCount { get; set; }

        [Bindable(true)]
        public int PaymentCount { get; set; }

        [Bindable(true)]
        public int CreditCount { get; set; }

        [Bindable(true)]
        public int AppliedCount { get; set; }

        [Bindable(true)]
        public decimal InvoiceTotal { get; set; }

        [Bindable(true)]
        public decimal FinanceChargeTotal { get; set; }

        [Bindable(true)]
        public decimal PaymentTotal { get; set; }

        [Bindable(true)]
        public decimal CreditTotal { get; set; }

        [Bindable(true)]
        public decimal AppliedTotal { get; set; }

        [Bindable(true)]
        public decimal BalanceDue { get; set; }

        [Bindable(true)]
        public EntityList<OpenInvoice> InvoiceList { get; } = new EntityList<OpenInvoice>();

        [Bindable(true)]
        public EntityList<OpenInvoice> CreditMemoList { get; } = new EntityList<OpenInvoice>();

        [Bindable(true)]
        public EntityList<OpenInvoice> PaymentList { get; } = new EntityList<OpenInvoice>();

        [Bindable(true)]
        public EntityList<OpenInvoice> FinanceChargeList { get; } = new EntityList<OpenInvoice>();
        #endregion Properties
    }
}
