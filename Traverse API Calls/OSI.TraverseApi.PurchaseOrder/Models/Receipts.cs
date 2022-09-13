#region Using Directives
using System.Collections.Generic;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Models
{
    public class Receipts
    {
        #region Static
        public static decimal GetQty(int entryNum)
        {
            decimal num = 0m;
            foreach (TransactionLotReceipt transactionLotReceipt in GetAllReceiptsDetail(entryNum))
            {
                num += transactionLotReceipt.QtyFilled.Value;
            }
            return num;
        }

        public static List<TransactionLotReceipt> GetAllReceiptsDetail(int entrynum)
        {
            List<TransactionLotReceipt> list = new List<TransactionLotReceipt>();
            if (ReceiptHeaderList != null && entrynum > 0)
            {
                foreach (TransactionReceipt transactionReceipt in ReceiptHeaderList)
                {
                    if (transactionReceipt.LotReceiptList.Count > 0)
                    {
                        EntityList<TransactionLotReceipt> entityList = transactionReceipt.LotReceiptList.FindAll(TransactionLotReceiptBase.Columns.EntryNum, entrynum);
                        if (entityList != null)
                        {
                            list.AddRange(entityList);
                        }
                    }
                }
            }
            return list;
        }

        public static void SynchronizeProjectActivity(TransactionReceipt header)
        {
            foreach (TransactionLotReceipt current in header.LotReceiptList)
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
            EntityList<TransactionReceipt> receiptList = ((TransactionHeader)detail.Parent).GetReceiptList();
            if (receiptList == null || receiptList.Count == 0)
            {
                return false;
            }
            foreach (TransactionReceipt current in receiptList)
            {
                if (current.LotReceiptList.Count > 0 && current.LotReceiptList.Find(TransactionLotReceiptBase.Columns.EntryNum, detail.EntryNum) != null)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion Static

        #region Properties
        public static EntityList<TransactionReceipt> ReceiptHeaderList { get; set; }
        #endregion Properties
    }
}
