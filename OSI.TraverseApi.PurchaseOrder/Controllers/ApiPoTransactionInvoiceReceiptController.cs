#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.PurchaseOrder.Controllers
{
    public class ApiPoTransactionInvoiceReceiptController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{id?}", typeof(TransactionInvoiceReceipt))]
        public async Task<IHttpActionResult> Get(string transId, string invcNum, int entryNum, string id = null)
        {
            return Ok(await Load(transId, invcNum, entryNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{id?}", typeof(TransactionInvoiceReceipt))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId, string invcNum, int entryNum, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, invcNum, entryNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{id?}", typeof(TransactionInvoiceReceipt))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, string invcNum, int entryNum, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, invcNum, entryNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{id}", typeof(TransactionInvoiceReceipt))]
        public async Task Delete(string transId, string invcNum, int entryNum, string id)
        {
            await this.MarkToDelete(transId, invcNum, entryNum, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected virtual void LoadHeader(string transId)
        {
            if (this.POHeader == null || !StringHelper.AreEqual(this.POHeader.TransId, transId, false))
            {
                this.POHeader = EntityProvider.GetEntity<TransactionHeader, TransactionHeaderProvider>(new string[] { transId }, this.CompId, null);
            }
        }

        protected virtual async Task Load(string transId, string invcNum, int entryNum)
        {
            LoadHeader(transId);
            if (this.CurrentInvoice == null
                || !StringHelper.AreEqual(this.CurrentInvoice.TransId, transId, false) || !StringHelper.AreEqual(this.CurrentInvoice.InvcNum, invcNum, false))
            {
                EntityList<TransactionInvoiceTotal> invoiceList = new EntityList<TransactionInvoiceTotal>();

                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);

                var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (list?.Count > 0)
                {
                    await this.FilterEntityListAsync(list, ApiPoTransactionHeaderController.FunctionID);
                    invoiceList = list[0]?.GetInvoiceList();

                    if (invoiceList.Count > 0)
                    {
                        await this.FilterEntityListAsync(invoiceList, ApiPoTransactionInvoiceController.FunctionID);

                        this.CurrentInvoice = invoiceList.Find(x => StringHelper.AreEqual(x.InvcNum, invcNum, false));

                        if (this.CurrentInvoice != null)
                            this.Provider.Items.Add(this.CurrentInvoice);
                    }

                    if (invoiceList.Count <= 0 || this.CurrentInvoice == null)
                        throw new InvalidValueException("Invoice Number could not be found.");

                    this.CurrentTransactionInvoice = (this.CurrentInvoice.DetailList as EntityList<TransactionInvoice>).Find(x => x.EntryNum == entryNum);

                    if (this.CurrentTransactionInvoice == null)
                        throw new InvalidValueException("Entry Number could not be found.");
                }
                else
                    throw new InvalidValueException("Transaction ID could not be found.");
            }
        }

        protected virtual async Task<EntityList<TransactionInvoiceReceipt>> Load(string transId, string invcNum, int entryNum, string id)
        {
            var list = this.CurrentTransactionInvoice?.InvoiceReceiptList as EntityList<TransactionInvoiceReceipt>;

            if (this.CurrentInvoice == null || !StringHelper.AreEqual(this.CurrentInvoice.TransId, transId, false)
                || !StringHelper.AreEqual(this.CurrentInvoice.InvcNum, invcNum, false))
            {
                await Load(transId, invcNum, entryNum);

                list = this.CurrentTransactionInvoice.InvoiceReceiptList as EntityList<TransactionInvoiceReceipt>;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                list = list?.FindAll(x => StringHelper.AreEqual(x.TransactionLotReceipt.TransactionReceipt?.ReceiptNum, id, true));

            return list;
        }

        protected virtual async Task<TransactionInvoiceReceipt> Find(string transId, string invcNum, int entryNum, string id)
        {
            var list = await Load(transId, invcNum, entryNum, id);
            return list?.Count > 0 ? list[0] : null;
        }

        protected virtual async Task<List<TransactionInvoiceReceipt>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string invcNum, int entryNum, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Receipt ID is provided along with more than one record.");

            var entityList = new List<TransactionInvoiceReceipt>();

            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, invcNum, entryNum, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);

            this.Provider?.Update(this.CompId);

            this.UpdateHeaderType();

            return entityList;
        }

        protected virtual async Task<TransactionInvoiceReceipt> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string invcNum, int entryNum, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReceiptId) || string.IsNullOrEmpty(bodyItem.ReceiptId))
                bodyItem.ReceiptId = code;
            else
                code = bodyItem.ReceiptId;

            var entity = await this.Find(transId, invcNum, entryNum, code);

            if (this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo == null || this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo?.InventoryType == InventoryType.Serial)
                throw new InvalidValueException("Current invoice does not support adding serial items.");

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentTransactionInvoice?.InvoiceReceiptList.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Receipt ID '{0}' could not be found on entry number '{1}', invoice number '{2}' and transaction '{3}'.", code, entryNum, invcNum, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.Qty = (ApiUserSkipped.IsApiUserSkipped(bodyItem.Qty)) ? entity.TransactionLotReceipt.QtyFilled : (Convert.ToDecimal(bodyItem.Qty) ?? 0m);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, string invcNum, int entryNum, string id)
        {
            var entity = await this.Find(transId, invcNum, entryNum, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Receipt ID '{0}' could not be found on entry number '{1}', invoice number '{2}' and transaction '{3}'.", id, entryNum, invcNum, transId));

            this.CurrentTransactionInvoice?.InvoiceReceiptList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void QtyPropertyChanged(TransactionInvoiceReceipt entity)
        {
            if (entity.Qty > entity.TransactionLotReceipt.QtyFilled)
            {
                if (POHeader.TransactionType > POTransactionType.Request)
                {
                    throw new InvalidValueException("Invoice Quantity cannot be greater than Receipt Quantity.");
                }
                else
                {
                    throw new InvalidValueException("Debit Memo Quantity cannot be greater than Return Quantity.");
                }
            }
        }

        protected virtual void LookupReceiptId(dynamic model, ApiEntityPropertyChangingArgs args)
        {
            var list = TransactionReceiptProvider.GetEntityList(this.CurrentTransactionInvoice.TransId, this.CurrentTransactionInvoice.CompId);
            var receipt = list.Find(rcpt => 
                    rcpt.ReceiptNum.Equals(model.ReceiptId as string, StringComparison.OrdinalIgnoreCase) &&
                    rcpt.LotReceiptList != null &&
                    rcpt.LotReceiptList.Exists(n => n.EntryNum == this.CurrentTransactionInvoice.EntryNum));
            if (receipt == null)
                throw new InvalidValueException(string.Format("Receipt number '{0}' cannot be found.", model.ReceiptId));

            args.ActualValue = receipt.LotReceiptList.Find(n => n.EntryNum == this.CurrentTransactionInvoice.EntryNum).ReceiptId;
        }

        protected virtual void UpdateHeaderType()
        {
            if ((this.POHeader.TransactionType == POTransactionType.NewOrder ||
            this.POHeader.TransactionType == POTransactionType.GoodsReceived) &&
            this.POHeader.GetInvoiceList().Count > 0)
            {
                this.POHeader.SetDefaultTransType(POTransactionType.InvoiceReceived);
            }
            else if (this.POHeader.TransactionType == POTransactionType.NewReturn &&
            this.POHeader.GetInvoiceList().Count > 0)
            {
                this.POHeader.SetDefaultTransType(POTransactionType.DebitMemo);
            }
            this.TransHeaderProvider?.Items.Add(POHeader);
            this.TransHeaderProvider?.Update(this.CompId);
        }
        #endregion Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Invoice Non Serial Property Changed
            PropertyDictionary.Add(TransactionInvoiceReceiptBase.Columns.Qty.ToString(), QtyPropertyChanged);

            //Lookup ReceiptId
            EntityPropertyDictionary.Add(TransactionInvoiceReceiptBase.Columns.ReceiptId.ToString(), LookupReceiptId);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.FieldName == TransactionInvoiceReceiptBase.Columns.ReceiptId.ToString())
            {
                args.ActualValue = (args.Entity as TransactionInvoiceReceipt)?.TransactionLotReceipt.RcptNum;
            }
        }
        #endregion Overrides

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as TransactionInvoiceReceipt;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionInvoiceReceipt> action = null;

            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);

            entity.PropertyChanged += Entity_PropertyChanged;
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider TransHeaderProvider { get; } = new TransactionHeaderProvider();

        protected TransactionInvoiceTotalProvider Provider { get; } = new TransactionInvoiceTotalProvider();

        protected TransactionInvoiceTotal CurrentInvoice { get; set; }
        protected TransactionInvoice CurrentTransactionInvoice { get; set; }

        protected TransactionInvoiceReceipt CurrentInvoiceReceipt { get; set; }

        protected SortedDictionary<string, Action<TransactionInvoiceReceipt>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionInvoiceReceipt>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        
        protected TransactionHeader POHeader
        {
            get;
            private set;
        }
        #endregion Properties

        #region Fields
        public const string FunctionID = "7255519d-c224-4d3a-bc82-c8f8e5c6a3ec";
        #endregion Fields
    }
}
