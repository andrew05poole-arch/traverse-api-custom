#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsPayable;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsPayable.Controllers
{
    public class ApiApTransactionOrderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(TransactionHeader), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id}", typeof(TransactionHeader), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(TransactionHeaderBase.Columns.VendorId.ToString(), VendorPropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CurrencyId.ToString(), (entity) => entity.UpdateExchangeRate());
            PropertyDictionary.Add(TransactionHeaderBase.Columns.TermsCode.ToString(), UpdateTransactionDates);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.InvoiceDate.ToString(), InvoiceDatePropertyChanged);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CashDiscFgn.ToString(), (entity) => entity.SetPayments(0));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.PrepaidAmtFgn.ToString(), ProcessPrepayment);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.PmtAmt1Fgn.ToString(), (entity) => entity.SetPayments(1));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.PmtAmt2Fgn.ToString(), (entity) => entity.SetPayments(2));
            PropertyDictionary.Add(TransactionHeaderBase.Columns.BankId.ToString(), UpdateBankInfo);
            PropertyDictionary.Add(TransactionHeaderBase.Columns.CheckDate.ToString(), (entity) => { if (entity.PrepaidAmtFgn.GetValueOrDefault() > 0) entity.SetPaymentFiscalPeriodYearFromDate(entity.CheckDate.GetValueOrDefault()); });
            EntityPropertyDictionary.Add("NetSalesTaxFgn", NetSalesTaxChanging);

            //Line Item Property Changes
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.WhseId.ToString(), WhsePropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.Qty.ToString(), QtyPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostFgn.ToString(), UnitCostPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.ExtCostFgn.ToString(), ExtCostPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.Units.ToString(), UnitPropertyChanged);
            DetailPropertyDictionary.Add(TransactionDetailBase.Columns.PartId.ToString(), ItemIdPropertyChanged);

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionLotBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionLotBase.Columns.CostUnitFgn.ToString(), (entity) => entity.CalculateBaseCost());

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), (entity) => entity.CalculateBaseCost());
        }

        protected virtual async Task<EntityList<TransactionHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.TransId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<TransactionHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                    builder.AppendEquals(TransactionHeaderBase.Columns.TransId, id);
                    var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<TransactionHeader> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.TransId, id, false));
        }

        protected virtual async Task<List<TransactionHeader>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Transaction ID is provided along with more than one record.");

            var entityList = new List<TransactionHeader>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || string.IsNullOrWhiteSpace(bodyItem.TransId))
                bodyItem.TransId = code;
            else
                code = bodyItem.TransId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new TransactionHeader(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (string.IsNullOrWhiteSpace(entity.TransId))
                entity.TransId = Provider.GetNextTransId();

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transaction '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailList")
            {
                if (((TransactionHeader)args.ParentObject).IsNew)
                    return this.CreateDetail((TransactionHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((TransactionHeader)args.ParentObject, args.ItemModel);
            }
            else
            {
                var lineItem = (TransactionDetailLineItem)args.ParentObject;
                if (args.PropertyName == "LotList")
                {
                    if (lineItem.INItemInfo == null || !lineItem.INItemInfo.IsLotted || lineItem.INItemInfo.InventoryType != InventoryType.Regular)
                        throw new InvalidValueException("This item is not a regular lotted item and cannot be processed");

                    if (lineItem.IsNew)
                        return this.CreateLot(lineItem, args.ItemModel);
                    else
                        return this.UpdateLot(lineItem, args.ItemModel);
                }
                else if (args.PropertyName == "SerialList")
                {
                    if (lineItem.INItemInfo == null || lineItem.INItemInfo.InventoryType != InventoryType.Serial)
                        throw new InvalidValueException("This item is not a serialized item and cannot be processed");

                    if (lineItem.IsNew)
                        return this.CreateSerial(lineItem, args.ItemModel);
                    else
                        return this.UpdateSerial(lineItem, args.ItemModel);
                }
            }
            return null;
        }

        protected virtual TransactionDetailLineItem UpdateDetail(TransactionHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry number is required.");

            this.FilterEntityList(parent.DetailList, ApiApTransactionDetailController.FunctionId);
            TransactionDetailLineItem entity = (parent.DetailList as EntityList<TransactionDetailLineItem>).Find(TransactionDetailBase.Columns.EntryNum, (int)bodyItem.EntryNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Line item '{0}' with Transaction ID '{1}' could not be found.", bodyItem.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LineItemUpdateComplete;
            entity.PropertyChanged += LineItem_PropertyChanged;
            

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionDetailLineItem CreateDetail(TransactionHeader parent, dynamic bodyItem)
        {
            TransactionDetailLineItem entity = (parent.DetailList as EntityList<TransactionDetailLineItem>).AddNew();

            entity.LineSeq = entity.EntryNum;
            entity.LocationId = entity.WhseId;
            string gLAcct = parent.VendorInfo.GLAcct;
            if (string.IsNullOrEmpty(gLAcct))
                entity.GLAcct = Utility.GlAcctInv;
            else
                entity.GLAcct = gLAcct;

            entity.Quantity = 1M;
            entity.Unit = Utility.SMDefaultUnit;
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LineItemUpdateComplete;
            entity.PropertyChanged += LineItem_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionLot UpdateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Sequence number is required.");

            this.FilterEntityList(parent.LotList, ApiApTransactionLotController.FunctionId);
            TransactionLot entity = parent.LotList.Find(x => x.SeqNum == (int)bodyItem.SeqNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Lot '{0}' for Line item '{1}' with Transaction ID '{2}' could not be found.", bodyItem.SeqNum, parent.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionLot CreateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            TransactionLot entity = parent.LotList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionSerial UpdateSerial(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            this.FilterEntityList(parent.SerialList, ApiApTransactionSerialController.FunctionId);
            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Line item '{1}' with Transaction ID '{2}' could not be found.", 
                    bodyItem.SerNum, parent.EntryNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionSerial CreateSerial(TransactionDetailLineItem parent, dynamic bodyItem)
        {
             if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Line item '{1}' with Transaction ID '{2}' already exists.", 
                    bodyItem.SerNum, parent.EntryNum, parent.TransId));

            entity = (parent.SerialList as EntityList<TransactionSerial>).AddNew();

            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }        

        protected virtual Lot CreateBrandNewLot(string itemId, string locId, string lotNum)
        {
            Lot newLot = new Lot(this.CompId)
            {
                LotNum = lotNum,
                LocId = locId,
                ItemId = itemId,
                InitialDate = DateTime.Today
            };
            return newLot;
        }

        #region Header Update Methods
        protected virtual void VendorPropertyChanged(TransactionHeader entity)
        {
            if (entity.VendorInfo != null)
                entity.CurrencyId = entity.VendorInfo.CurrencyId;

            entity.SetVendorDefaults();
            entity.UpdateExchangeRate();
        }

        protected virtual void InvoiceDatePropertyChanged(TransactionHeader entity)
        {
            if (entity.IsNew)
            {
                entity.SetExchangeRateBase();
                entity.UpdateExchangeRate();
            }
            entity.SetFiscalPeriodYearFromDate(entity.InvoiceDate.GetValueOrDefault(DateTime.Today));
            UpdateTransactionDates(entity);
        }

        protected virtual void UpdateTransactionDates(TransactionHeader entity)
        {
            entity.SetDiscountDate(entity.InvoiceDate.GetValueOrDefault(DateTime.Today));
            entity.SetNetDueDate(entity.InvoiceDate.GetValueOrDefault(DateTime.Today));
        }

        protected virtual void ProcessPrepayment(TransactionHeader entity)
        {
            if (entity.PrepaidAmtFgn.GetValueOrDefault() == 0)
                entity.SetPaymentDefaults();

            entity.SetPrePayExchangeRateBase();
            entity.SetPayments(0);
        }

        protected virtual void UpdateBankInfo(TransactionHeader entity)
        {
            entity.SetBankDefaults();
            entity.SetPayments(0);
        }

        protected virtual void NetSalesTaxChanging(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            TransactionHeader header = e.Entity as TransactionHeader; 
            e.Handled = true;

            if (header == null || !header.TaxableYn.GetValueOrDefault() || ApiUserSkipped.IsApiUserSkipped(bodyItem.NetSalesTaxFgn))
                return;

            decimal.TryParse(bodyItem.NetSalesTaxFgn.ToString(), out decimal taxValue);
            header.TaxAdjAmtFgn = taxValue - header.SalesTaxFgn;
        }
        #endregion Header Update Methods

        #region Line Item Update Methods
        protected virtual void WhsePropertyChanged(TransactionDetailLineItem entity)
        {
            entity.ExtLocA = null;
            entity.SetItemLocationDefault();
            entity.SetDefaultBin();
            entity.CalculateExtendedCost();
        }

        protected virtual void QtyPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.SetCostDefault();
            ReAllocateTransactions(false, entity);
            entity.CalculateExtendedCost();
        }

        protected virtual void UnitCostPropertyChanged(TransactionDetailLineItem entity)
        {
            ReAllocateTransactions(false, entity);
            entity.CalculateExtendedCost();
        }

        protected virtual void ExtCostPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CalculateUnitCost();
            ReAllocateTransactions(true, entity);
        }

        protected virtual void UnitPropertyChanged(TransactionDetailLineItem entity)
        {
            if (entity.LotList.Count == 0 && (entity.SerialList?.Count).GetValueOrDefault() == 0)
                entity.SetCostDefault();

            if (entity.INItemInfo != null && entity.INItemInfo.IsLotted && entity.INItemInfo.InventoryType != InventoryType.Serial)
            {
                foreach (TransactionLot lot in entity.LotList)
                    lot.Unit = entity.Unit;
            }
        }

        protected virtual void ItemIdPropertyChanged(TransactionDetailLineItem entity)
        {
            if (entity.INItemInfo != null && entity.INItemInfo.InventoryStatus == InventoryStatus.Superseded && !string.IsNullOrEmpty(entity.INItemInfo.SuperId))
            {
                entity.PartId = entity.INItemInfo.SuperId;
            }

            entity.SetItemDefault();
        }

        protected virtual void LineItemUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionDetailLineItem;
            entity.PropertyChanged -= LineItem_PropertyChanged;

            var header = entity.Parent as TransactionHeader;
            TransactionHeader.Recalculate(header);
            header.CalculateTotals();

            if (ConfigurationValueProvider.GetRule<bool>(AppId.AP, ConfigurationValue.DiscAutoYn, this.CompId))
                header.CashDiscFgn = header.GetDiscountAmount();

            header.SetPaymentDefaults();
        }

        protected virtual void ReAllocateTransactions(bool useExtCost, TransactionDetailLineItem entity)
        {
            if (Utility.TransAllocYn
                && entity.AllocationList.Count > 0
                && entity.AllocationList[0].AllocationTransactionHeader != null)
            {
                entity.AllocateTransactions(useExtCost);
            }
            if (useExtCost)
                entity.Validate("ExtCostFgn");
        }
        #endregion Line Item Update Methods

        #region Lot Update Methods
        protected virtual void LotNumberPropertyChanged(TransactionLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum))
                return;

            entity.SetDefaults();
            entity.SetCostDefault();

            Lot lot = entity.INItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && ((TransactionHeader)entity.Parent).TransactionType == APTransactionType.Invoice && lot == null)
            {
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }
        }

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionLot;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
        }
        #endregion Lot Update Methods

        #region Serial Update Methods
        protected virtual void SerialNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum))
                return;

            var serial = entity.INItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            if (((TransactionHeader)entity.TransactionDetailLineItem.Parent).TransactionType == APTransactionType.Invoice)
            {
                if (serial != null)
                    throw new InvalidValueException(string.Format("Serial Number '{0}' already exists for this item.", entity.SerNum));

                entity.SetDefaults();
                entity.SetCostDefault();
                return;
            }

            if (serial == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' is not on file.", entity.SerNum));

            if (serial.SerialItemStatus == SerialItemStatus.Available)
            {
                serial.SerialItemStatus = SerialItemStatus.Returned;
                entity.SetDefaults();
                entity.SetCostDefault();
            }
            else
                throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Available to be returned.", entity.SerNum));
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum))
                return;

            Lot lot = entity.INItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && ((TransactionHeader)entity.TransactionDetailLineItem.Parent).TransactionType == APTransactionType.Invoice && lot == null)
            {
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }
        }

        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionSerial;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
        }
        #endregion Serial Update Methods
        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out Action<dynamic, ApiEntityPropertyChangingArgs> action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as TransactionHeader;
            entity.PropertyChanged -= Entity_PropertyChanged;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionHeader> action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void LineItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionDetailLineItem> action))
                action.Invoke(sender as TransactionDetailLineItem);
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionLot> action))
                action.Invoke(sender as TransactionLot);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out Action<TransactionSerial> action))
                action.Invoke(sender as TransactionSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected SortedDictionary<string, Action<TransactionHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionHeader>>();

        protected SortedDictionary<string, Action<TransactionDetailLineItem>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetailLineItem>>();

        protected SortedDictionary<string, Action<TransactionLot>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLot>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "29962cf5-40bd-477a-9f4e-17d0ef20824c";
        #endregion Fields
    }
}
