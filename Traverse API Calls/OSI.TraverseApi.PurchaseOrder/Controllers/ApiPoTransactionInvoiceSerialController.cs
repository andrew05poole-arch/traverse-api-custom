#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoTransactionInvoiceSerialController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{receiptid}/serial/{id?}", typeof(TransactionSerialInvoice))]
        public async Task<IHttpActionResult> Get(string transId, string invcNum, int entryNum, string receiptId, string id = null)
        {
            return Ok(await Load(transId, invcNum, entryNum, receiptId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{receiptid}/serial/{id?}", typeof(TransactionSerialInvoice))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId, string invcNum, int entryNum, string receiptId, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, invcNum, entryNum, receiptId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{receiptid}/serial/{id?}", typeof(TransactionSerialInvoice))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, string invcNum, int entryNum, string receiptId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, invcNum, entryNum, receiptId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{entrynum:int}/receiptid/{receiptid}/serial/{id}", typeof(TransactionSerialInvoice))]
        public async Task Delete(string transId, string invcNum, int entryNum, string receiptId, string id)
        {
            await this.MarkToDelete(transId, invcNum, entryNum, receiptId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Invoice Serial Property Changed
            EntityPropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), ValidateHandledProperty);
        }

        protected virtual async Task Load(string transId, string invcNum, int entryNum, string receiptId)
        {
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
                        throw new InvalidValueException($"Entry Number {entryNum} could not be found on Invoice {invcNum}");

                    this.CurrentInvoiceReceipt = (this.CurrentTransactionInvoice.InvoiceReceiptList.Find(x => StringHelper.AreEqual(x.TransactionLotReceipt.TransactionReceipt?.ReceiptNum, receiptId, false)));

                    if (this.CurrentInvoiceReceipt == null)
                        throw new InvalidValueException("Receipt Number could not be found.");
                }
                else
                    throw new InvalidValueException("Transaction ID could not be found.");
            }
        }

        protected virtual async Task<EntityList<TransactionSerialInvoice>> Load(string transId, string invcNum, int entryNum, string receiptId, string id)
        {
            var list = this.CurrentInvoiceReceipt?.SerialList as EntityList<TransactionSerialInvoice>;

            if (this.CurrentInvoice == null || !StringHelper.AreEqual(this.CurrentInvoice.TransId, transId, false)
                || !StringHelper.AreEqual(this.CurrentInvoice.InvcNum, invcNum, false))
            {
                await Load(transId, invcNum, entryNum, receiptId);

                list = this.CurrentInvoiceReceipt.SerialList as EntityList<TransactionSerialInvoice>;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                list = list?.FindAll(x => StringHelper.AreEqual(x.SerialNumber, id, false));

            return list;
        }

        protected virtual async Task<TransactionSerialInvoice> Find(string transId, string invcNum, int entryNum, string receiptId, string id)
        {
            var list = await Load(transId, invcNum, entryNum, receiptId, id);
            return list?.Find(x => StringHelper.AreEqual(x.SerialNumber, id, false));
        }

        protected virtual async Task<List<TransactionSerialInvoice>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string invcNum, int entryNum, string receiptId, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Serial Number is provided along with more than one record.");

            var entityList = new List<TransactionSerialInvoice>();

            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, invcNum, entryNum, receiptId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);
            this.SerialProvider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionSerialInvoice> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string invcNum, int entryNum, string receiptId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerialNumber) || string.IsNullOrEmpty(bodyItem.SerialNumber))
                bodyItem.SerialNumber = code;
            else
                code = bodyItem.SerialNumber;

            var entity = await this.Find(transId, invcNum, entryNum, receiptId, code);

            if (this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo == null || this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo?.InventoryType != InventoryType.Serial)
                throw new InvalidValueException("Current invoice does not support adding non serial items.");

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = (this.CurrentInvoiceReceipt?.SerialList as EntityList<TransactionSerialInvoice>).AddNew();    
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' could not be found on entry number '{1}', invoice number '{2}' and transaction '{3}'.", code, entryNum, invcNum, transId));

            this.CurrentSerialInvoice = entity;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);               
            this.UpdateSerialTransaction(bodyItem, code);
            this.CurrentInvoiceReceipt.CalculateTotal();
            this.CurrentInvoiceReceipt.TransactionInvoice.CalculateQty();
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, string invcNum, int entryNum, string receiptId, string id)
        {
            var entity = await this.Find(transId, invcNum, entryNum, receiptId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' could not be found on entry number '{1}', invoice number '{2}' and transaction '{3}'.", id, entryNum, invcNum, transId));

            this.CurrentSerialInvoice = entity;
            EntityList<TransactionSerial> serial = GetTransactionSerial();
            serial[0].InvcNum = null;
            this.CurrentInvoiceReceipt.CalculateTotal();
            this.CurrentInvoiceReceipt.TransactionInvoice.CalculateQty();
            this.SerialProvider.Items.Add(serial[0]);
            this.SerialProvider?.Update(this.CompId);
        }

        protected virtual void ValidateHandledProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionInvoice lotInvoice)
                {
                    if (lotInvoice.TransactionDetail.INItemInfo.InventoryType == InventoryType.Serial)
                        e.Handled = true;
                }
            }
        }

        protected virtual EntityList<TransactionSerial> GetTransactionSerial()
        {
            EntityList<TransactionSerial> transactionSerial = new EntityList<TransactionSerial>(this.CompId);
            var builder = new SqlFilterBuilder<TransactionSerialBase.Columns>();
            builder.AppendEquals(TransactionSerialBase.Columns.SerNum, this.CurrentSerialInvoice.SerialNumber);

            return new TransactionSerialProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
        }
        protected virtual void UpdateSerialTransaction(dynamic bodyItem, string code)
        {
            this.CurrentSerialInvoice.SerialNumber = code;
            EntityList<TransactionSerial> transactionSerial = GetTransactionSerial();
            this.CurrentSerialInvoice.InvoiceUnitCostFgn = (ApiUserSkipped.IsApiUserSkipped(bodyItem.InvoiceUnitCostFgn)) ? transactionSerial[0].RcptUnitCost : bodyItem.InvoiceUnitCostFgn;
            if(this.CurrentSerialInvoice.IsNew)
                transactionSerial[0].InvcNum = this.CurrentInvoice.InvcNum;
            transactionSerial[0].InvcUnitCostFgn = this.CurrentSerialInvoice.InvoiceUnitCostFgn;
            transactionSerial[0].CalculateInvoiceBaseCost();
            this.CurrentSerialInvoice.InvoiceUnitCost = transactionSerial[0].InvcUnitCost ?? 0m;
            this.SerialProvider.Items.Add(transactionSerial[0]);
        }
        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as TransactionSerial;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionSerial> action = null;

            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);

            entity.PropertyChanged += Entity_PropertyChanged;
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionInvoiceTotalProvider Provider { get; } = new TransactionInvoiceTotalProvider();
        private TransactionSerialProvider SerialProvider { get; } = new TransactionSerialProvider();
        protected TransactionInvoiceTotal CurrentInvoice { get; set; }
        protected TransactionInvoice CurrentTransactionInvoice { get; set; }
        protected TransactionInvoiceReceipt CurrentInvoiceReceipt { get; set; }
        protected TransactionSerialInvoice CurrentSerialInvoice { get; set; }
        protected SortedDictionary<string, Action<TransactionSerial>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "a9853003-6824-46d6-ba23-5727e0078c44";
        #endregion Fields
    }
}
