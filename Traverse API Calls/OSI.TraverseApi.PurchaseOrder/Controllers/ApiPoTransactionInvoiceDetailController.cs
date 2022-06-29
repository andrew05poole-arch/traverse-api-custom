#region Using Directives
using TRAVERSE.Web.API.PurchaseOrder.Controllers;
using TRAVERSE.Web.API.PurchaseOrder.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder
{
    public class ApiPoInvoiceDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{id:int?}", typeof(TransactionInvoice))]
        public async Task<IHttpActionResult> Get(string transId = null, string invcNum = null, int? id = null)
        {
            return Ok(await Load(transId, invcNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{id:int?}", typeof(TransactionInvoice))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId = null, string invcNum = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, invcNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{id:int?}", typeof(TransactionInvoice))]
        public async Task<IHttpActionResult> Post([FromBody] dynamic body, string transId = null, string invcNum = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, invcNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/invoice/{invcnum}/entrynum/{id:int}", typeof(TransactionInvoice))]
        public async Task Delete(string transId, string invcNum, int id)
        {
            await this.MarkToDelete(transId, invcNum, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Invoice Detail Property Changed
            PropertyDictionary.Add(TransactionInvoiceBase.Columns.EntryNum.ToString(), EntryNumPropertyChanged);
            PropertyDictionary.Add(TransactionInvoiceBase.Columns.Qty.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            PropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            PropertyDictionary.Add(TransactionLotReceiptBase.Columns.ExtCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateUnitCost();
                entity.SuppressEntityEvents = false;
            });

            //Invoice Serial Property Changed
            EntityPropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), ValidateHandledProperty);

            //Invoice Non Serial Property Changed
            NonSerialPropertyDictionary.Add(TransactionInvoiceReceiptBase.Columns.Qty.ToString(), QtyPropertyChanged);
        }

        protected virtual async Task Load(string transId, string invcNum)
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
                }
                else
                    throw new InvalidValueException("Transaction ID could not be found.");
            }
        }
        protected virtual async Task<EntityList<TransactionInvoice>> Load(string transId, string invcNum, int? id)
        {
            var list = this.CurrentInvoice?.DetailList;

            if (this.CurrentInvoice == null || !StringHelper.AreEqual(this.CurrentInvoice.TransId, transId, false)
                || !StringHelper.AreEqual(this.CurrentInvoice.InvcNum, invcNum, false))
            {
                await Load(transId, invcNum);

                list = this.CurrentInvoice?.DetailList;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                list = (list as EntityList<TransactionInvoice>)?.FindAll(TransactionInvoiceBase.Columns.EntryNum, id.Value, true);

            return list as EntityList<TransactionInvoice>;
        }

        protected virtual async Task<TransactionInvoice> Find(string transId, string invcNum, int? id)
        {
            var list = await Load(transId, invcNum, id);
            return list?.Find(x =>  x.EntryNum == id.Value);
        }

        protected virtual async Task<List<TransactionInvoice>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string invNum, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Entry Number is provided along with more than one record.");

            var entityList = new List<TransactionInvoice>();
            this.TransId = transId;

            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, invNum, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);
            this.SerialProvider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionInvoice> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string invcNum, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum) || bodyItem.EntryNum == null)
                bodyItem.EntryNum = code;
            else
                code = Convert.ToInt32(bodyItem.EntryNum);

            var entity = await this.Find(transId, invcNum, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = (this.CurrentInvoice.DetailList as EntityList<TransactionInvoice>).AddNew();
                entity.InvoiceId = Guid.NewGuid();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Entry Number '{0}' could not be found on invoice number '{1}' and transaction '{2}'.", code, invcNum, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }
        protected virtual async Task MarkToDelete(string transId, string invNum, int id)
        {
            var entity = await this.Find(transId, invNum, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Entry Number '{0}' could not be found on invoice number '{1}' and transaction '{2}'.", id, invNum, transId));

            this.CurrentInvoice.DetailList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
             if (args.PropertyName == "InvoiceReceiptList")
            {
                var transactionInvoice = (TransactionInvoice)args.ParentObject;
                this.CurrentTransactionInvoice = transactionInvoice;

                if (transactionInvoice.IsNew)
                    return this.CreateInvoiceReceipt(transactionInvoice, args.ItemModel);
                else
                    return this.UpdateInvoiceReceipt(transactionInvoice, args.ItemModel);
            }
            else if (args.PropertyName == "SerialList")
            {               
                var lotInvoice = (TransactionInvoiceReceipt)args.ParentObject;

                if (this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo == null || this.CurrentTransactionInvoice.TransactionDetail?.INItemInfo?.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("Current invoice does not support adding serial items.");

                if (lotInvoice.IsNew)
                    return this.CreateSerial(lotInvoice, args.ItemModel);
                else
                    return this.UpdateSerial(lotInvoice, args.ItemModel);
            }

            return null;
        }

        #region Invoice Detail Update Methods
        protected virtual void EntryNumPropertyChanged(TransactionInvoice entity)
        {
            entity.LotNumber = null;
            entity.SetDefaults();

            Invoices.InvoiceHeaderList = this.Provider.Items;
            decimal? qtyFilled = entity.Qty;
            decimal d = 0m;
            if ((qtyFilled.GetValueOrDefault() == d & qtyFilled != null) && entity.TransactionDetail != null)
            {
                decimal num = entity.TransactionDetail.QtyOrd.Value - Invoices.GetQty(entity.EntryNum);

                if (num >= 0m)
                    entity.Qty = new decimal?(num);
            }
            entity.SetCostDefault();
        }
        #endregion Invoice Detail Update Methods

        #region Serial Update Methods
        protected virtual TransactionSerialInvoice UpdateSerial(TransactionInvoiceReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerialNumber))
                throw new InvalidValueException("Serial Number is required.");

            this.FilterEntityList(parent.SerialList, ApiPoTransactionInvoiceSerialController.FunctionID);

            this.CurrentSerialInvoice = (parent.SerialList as EntityList<TransactionSerialInvoice>)?.Find(x => StringHelper.AreEqual(x.SerialNumber, bodyItem.SerialNumber, false));
            if (this.CurrentSerialInvoice == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' could not be found.", bodyItem.SerialNumber));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            this.CurrentSerialInvoice.PropertyChanged += SerialEntity_PropertyChanged;
            this.CurrentSerialInvoice.InvoiceUnitCostFgn = (ApiUserSkipped.IsApiUserSkipped(bodyItem.InvoiceUnitCostFgn)) ? this.CurrentSerialInvoice.InvoiceUnitCostFgn : bodyItem.InvoiceUnitCostFgn;
            parent.CalculateTotal();
            parent.TransactionInvoice.CalculateQty();
            this.UpdateSerialTransaction();

            Request.RegisterForDispose(this.CurrentSerialInvoice);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return this.CurrentSerialInvoice;
        }

        protected virtual TransactionSerialInvoice CreateSerial(TransactionInvoiceReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerialNumber))
                throw new InvalidValueException("Serial Number is required.");

            this.CurrentSerialInvoice = (parent.SerialList as EntityList<TransactionSerialInvoice>)?.Find(x => StringHelper.AreEqual(x.SerialNumber, bodyItem.SerialNumber, false));
            if (this.CurrentSerialInvoice != null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' already exists.",
                    bodyItem.SerialNumber));

            this.CurrentSerialInvoice = (parent.SerialList as EntityList<TransactionSerialInvoice>).AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            this.CurrentSerialInvoice.PropertyChanged += SerialEntity_PropertyChanged;
            this.CurrentSerialInvoice.SerialNumber = bodyItem.SerialNumber;
            EntityList<TransactionSerial> transactionSerial = this.GetTransactionSerial();
            this.CurrentSerialInvoice.InvoiceUnitCostFgn = (ApiUserSkipped.IsApiUserSkipped(bodyItem.InvoiceUnitCostFgn)) ? transactionSerial[0].RcptUnitCost : bodyItem.InvoiceUnitCostFgn;
            parent.CalculateTotal();
            parent.TransactionInvoice.CalculateQty();

            Request.RegisterForDispose(this.CurrentSerialInvoice);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return this.CurrentSerialInvoice;
        }

        protected virtual EntityList<TransactionSerial> GetTransactionSerial()
        {
            EntityList<TransactionSerial> transactionSerial = new EntityList<TransactionSerial>(this.CompId);
            var builder = new SqlFilterBuilder<TransactionSerialBase.Columns>();
            builder.AppendEquals(TransactionSerialBase.Columns.SerNum, this.CurrentSerialInvoice.SerialNumber);

            return new TransactionSerialProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
        }
        protected virtual void UpdateSerialTransaction()
        {
            EntityList<TransactionSerial> transactionSerial = GetTransactionSerial();
            transactionSerial[0].InvcUnitCostFgn = this.CurrentSerialInvoice.InvoiceUnitCostFgn;
            transactionSerial[0].CalculateInvoiceBaseCost();
            this.CurrentSerialInvoice.InvoiceUnitCost = transactionSerial[0].InvcUnitCost ?? 0m;
            this.SerialProvider.Items.Add(transactionSerial[0]);
        }
        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionSerialInvoice;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
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
        #endregion Serial Update Methods

        #region Non Serial Update Methods
        protected virtual TransactionInvoiceReceipt UpdateInvoiceReceipt(TransactionInvoice parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReceiptId))
                throw new InvalidValueException("Receipt ID is required.");

            this.FilterEntityList(parent.InvoiceReceiptList, ApiPoTransactionInvoiceReceiptController.FunctionID);

            TransactionInvoiceReceipt entity = parent.InvoiceReceiptList?.Find(x => StringHelper.AreEqual(x.ReceiptId.ToString(), bodyItem.ReceiptId, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Receipt ID '{0}' could not be found.", bodyItem.ReceiptId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = NonSerialUpdateComplete;
            entity.PropertyChanged += NonSerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }
        protected virtual TransactionInvoiceReceipt CreateInvoiceReceipt(TransactionInvoice parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReceiptId))
                throw new InvalidValueException("Receipt ID is required.");

            TransactionInvoiceReceipt entity = parent.InvoiceReceiptList?.Find(x => StringHelper.AreEqual(x.ReceiptId.ToString(), bodyItem.ReceiptId, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Receipt ID '{0}' could not be found.", bodyItem.ReceiptId));

            entity = parent.InvoiceReceiptList.AddNew();
            entity.ReceiptId = Guid.Parse(bodyItem.ReceiptId);

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.Qty = (ApiUserSkipped.IsApiUserSkipped(bodyItem.Qty)) ? entity.TransactionLotReceipt.QtyFilled : (Convert.ToDecimal(bodyItem.Qty) ?? 0m);
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = NonSerialUpdateComplete;  
            entity.PropertyChanged += NonSerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }
        protected virtual void NonSerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionInvoiceReceipt;
            entity.PropertyChanged -= NonSerialEntity_PropertyChanged;
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
        #endregion Non Serial Update Methods
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
            var entity = sender as TransactionInvoice;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionInvoice> action = null;

            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);

            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSerialInvoice> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerialInvoice);
        }

        private void NonSerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionInvoiceReceipt> action = null;
            if (NonSerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionInvoiceReceipt);
        }
        #endregion Event Handlers

        #region Properties
        private TransactionInvoiceTotalProvider Provider { get; } = new TransactionInvoiceTotalProvider();
        private TransactionSerialProvider SerialProvider { get; } = new TransactionSerialProvider();
        protected TransactionInvoiceTotal CurrentInvoice { get; set; }
        protected TransactionInvoice CurrentTransactionInvoice { get; set; }
        protected TransactionSerialInvoice CurrentSerialInvoice { get; set; }
        protected SortedDictionary<string, Action<TransactionInvoice>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionInvoice>>();
        protected SortedDictionary<string, Action<TransactionSerialInvoice>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerialInvoice>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        protected SortedDictionary<string, Action<TransactionInvoiceReceipt>> NonSerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionInvoiceReceipt>>();

        protected string TransId { get; set; }       
        protected TransactionHeader POHeader
        {
            get
            {
                if (this._header == null)
                    this._header = EntityProvider.GetEntity<TransactionHeader, TransactionHeaderProvider>(new string[] { this.TransId }, this.CompId, null);

                return this._header;
            }
        }
        #endregion Properties

        #region Fields
        public const string FunctionID = "e34edcee-c7b4-40e7-bbe3-f74fbb36ee54";
        private TransactionHeader _header;
        #endregion Fields

    }
}
