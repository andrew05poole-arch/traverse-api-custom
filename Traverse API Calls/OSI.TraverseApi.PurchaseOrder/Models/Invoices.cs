#region Using Directives
using System;
using System.Collections.Generic;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Models
{
    public class Invoices
    {
        #region Static
        public static decimal GetQty(int entryNum)
        {
            decimal num = 0m;
            foreach (TransactionInvoice transactionInvoice in GetAllInvoicesDetail(entryNum))
            {
                num += transactionInvoice.Qty.Value;
            }
            return num;
        }

        public static List<TransactionInvoice> GetAllInvoicesDetail(int entrynum)
        {
            List<TransactionInvoice> list = new List<TransactionInvoice>();
            if (InvoiceHeaderList != null && entrynum > 0)
            {
                foreach (TransactionInvoiceTotal transactionInvoice in InvoiceHeaderList)
                {
                    if (transactionInvoice.DetailList.Count > 0)
                    {
                        EntityList<TransactionInvoice> entityList = (transactionInvoice.DetailList as EntityList<TransactionInvoice>).FindAll(TransactionInvoiceBase.Columns.EntryNum, entrynum);
                        if (entityList != null)
                        {
                            list.AddRange(entityList);
                        }
                    }
                }
            }
            return list;
        }

        public static void SynchronizeProjectActivity(TransactionInvoiceTotal header)
        {
            foreach (TransactionInvoice current in header.DetailList)
            {
                if (current.TransactionDetail != null && current.TransactionDetail.IsProjectItem)
                {
                    current.MarkAsDirty();
                }
            }
        }

        public static bool IsReceivedLineItem(TransactionDetail detail)
        {
            if (detail == null || detail.Parent == null)
            {
                return false;
            }
            EntityList<TransactionInvoiceTotal> invoiceList = ((TransactionHeader)detail.Parent).GetInvoiceList();
            if (invoiceList == null || invoiceList.Count == 0)
            {
                return false;
            }
            foreach (TransactionInvoiceTotal current in invoiceList)
            {
                if (current.DetailList.Count > 0 && (current.DetailList as EntityList<TransactionInvoice>).Find(TransactionInvoiceBase.Columns.EntryNum, detail.EntryNum) != null)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion Static

        #region Properties
        public static EntityList<TransactionInvoiceTotal> InvoiceHeaderList { get; set; }
        #endregion Properties
    }
}
