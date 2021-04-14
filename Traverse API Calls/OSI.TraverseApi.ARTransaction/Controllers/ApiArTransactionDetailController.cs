#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion

namespace OSI.TraverseApi.AccountsReceivable.Controllers
{
    public class ApiArTransactionDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Get(string transId, int? id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transId}/lineitem/{id:int}", typeof(TransactionDetailLineItem), new object[] { ApiArTransactionSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task Delete(string transId, int id)
        {
            await this.MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Line Item Property Changes
            PropertyDictionary.Add(TransactionDetailBase.Columns.PartId.ToString(), ItemIdPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.WhseId.ToString(), WhsePropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.AcctCode.ToString(), (entity) => { if (!string.IsNullOrWhiteSpace(entity.AcctCode)) entity.SetGLAccountDefaults(entity.AcctCode); });
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitsSell.ToString(), UnitsSellPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostSellFgn.ToString(), UnitCostPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitPriceSellBasisFgn.ToString(), (entity) => entity.CalculateExtendedPrice());
            PropertyDictionary.Add(TransactionDetailBase.Columns.QtyOrdSell.ToString(), QtyPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.QtyShipSell.ToString(), QtyShipPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.TaxClass.ToString(), (entry) => CurrentTransaction.ResetTaxAdjustment());

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionLotBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionLotBase.Columns.CostUnitFgn.ToString(), (entity) =>
            {
                entity.Calculate();
                entity.UpdateCosts(true);
            });

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), (entity) => entity.UpdateCosts(true));
        }

        protected virtual async Task<EntityList<TransactionDetailLineItem>> Load(string transId, int? id)
        {
            var list = CurrentTransaction?.DetailList as EntityList<TransactionDetailLineItem>;

            if (CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.TransId, transId, false))
            {
                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);
                builder.AppendEquals(TransactionHeaderBase.Columns.VoidYn, "0");
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiArTransactionOrderController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", id));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.DetailList as EntityList<TransactionDetailLineItem>;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(TransactionDetailBase.Columns.EntryNum, id.Value);

            return list;
        }

        protected virtual async Task<TransactionDetailLineItem> Find(string transId, int id)
        {
            var list = await Load(transId, id);
            return list.Find(x => x.EntryNum == id);
        }

        protected virtual async Task<List<TransactionDetailLineItem>> ProcessEditRequest(bool isCreate, dynamic body, string transId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Line Item is provided along with more than one record.");

            var entityList = new List<TransactionDetailLineItem>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            CurrentTransaction.CalculateTotals();
            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionDetailLineItem> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum) || bodyItem.EntryNum == null)
                bodyItem.EntryNum = code;
            else
                code = Convert.ToInt32(bodyItem.EntryNum);

            var entity = await this.Find(transId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentTransaction.DetailList.AddNew() as TransactionDetailLineItem;
                entity.LineSeq = entity.EntryNum;
                entity.Quantity = 1M;
                entity.QtyOrdSell = 1M;
                entity.QtyShipSell = 1M;
                entity.ReqShipDate = DateTime.Today;

                if (string.IsNullOrEmpty(CurrentTransaction.WhseId))
                {
                    string userLocationID = Utility.GetUserLocationID(CompId);
                    if (!string.IsNullOrEmpty(userLocationID))
                        entity.LocationId = userLocationID;
                    else
                        entity.LocationId = ConfigurationValue.GetRule<string>("SM", "WhseID", this.CompId);
                }
                else
                    entity.LocationId = CurrentTransaction.WhseId;
                
                entity.Unit = ConfigurationValue.GetRule<string>("SM", "Units", this.CompId);
                entity.SetGLAccountDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Line Item {0} not be found on transaction '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, int id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Line item {0} could not be found on transaction '{1}'", id, transId));

            CurrentTransaction.DetailList.Remove(entity);
            CurrentTransaction.CalculateTotals();
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "LotList")
            {
                if (((TransactionDetailLineItem)args.ParentObject).IsNew)
                    return this.CreateLot((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateLot((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
            }
            else if (args.PropertyName == "SerialList")
            {
                if (((TransactionDetailLineItem)args.ParentObject).IsNew)
                    return this.CreateSerial((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateSerial((TransactionDetailLineItem)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Line Item Update Methods
        protected virtual void WhsePropertyChanged(TransactionDetailLineItem entity)
        {
            if (string.IsNullOrEmpty(entity.LocationId))
                return;

            entity.ExtLocA = null;
            entity.SetGLAccountDefaults();
            entity.SetDefaultCost();
            entity.SetDefaultBin();
            entity.SetDefaultPrice();
            entity.Calculate();
        }

        protected virtual void QtyPropertyChanged(TransactionDetailLineItem entity)
        {
            if (entity.IsNew && entity.InItem != null)
            {
                if (entity.InItem.InventoryType != InventoryType.Serial || !entity.InItem.IsLotted)
                {
                    entity.QtyShipSell = entity.QtyOrdSell;
                }
            }
            else if (entity.IsNew && entity.InItem == null)
            {
                entity.QtyShipSell = entity.QtyOrdSell;
            }
            ValidateQty(entity);
            entity.SetDefaultCost();
            entity.SetDefaultPrice();
            entity.Calculate();
        }

        protected virtual void QtyShipPropertyChanged(TransactionDetailLineItem entity)
        {
            ValidateQty(entity);
            entity.SetDefaultCost();
            entity.SetDefaultPrice();
            entity.Calculate();
        }

        protected virtual void UnitCostPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CalculateExtendedCost();
            entity.UpdateCosts(true);
        }

        protected virtual void UnitsSellPropertyChanged(TransactionDetailLineItem entity)
        {
            if (entity.InItem == null)
                return;

            ValidateQty(entity);
            entity.SetDefaultPrice();
            entity.SetDefaultCost();
        }

        protected virtual void ItemIdPropertyChanged(TransactionDetailLineItem entity)
        {
            entity.CustomerPartNumber = null;
            if (Utility.INYN)
            {
                if (entity.InItem == null)
                    return;

                entity.Unit = null;
                entity.LocationId = string.Empty;
                entity.SetDefaults();

                if (entity.InItem.InventoryStatus == InventoryStatus.Obsolete)
                    throw new InvalidValueException(string.Format("Item '{0}' is obsolete and is not valid for this transaction", entity.ItemId));

                if (entity.InItem.AllLocations.Count <= 0)
                    throw new InvalidValueException(string.Format("Item '{0}' has no locations", entity.ItemId));

                if (entity.InItem.InventoryStatus == InventoryStatus.Superseded && !string.IsNullOrWhiteSpace(entity.InItem.SuperId))
                    entity.ItemId = entity.InItem.SuperId;

                if (entity.InItem == null)
                    return;

                if (entity.InItem.IsKit)
                    throw new InvalidValueException(string.Format("Item '{0}' is a kitted item. Kitted items are not valid for this transaction", entity.ItemId));

                this.ValidateQty(entity);

                if (entity.InItem.IsLotted || entity.InItem.InventoryType == InventoryType.Serial)
                {
                    entity.Quantity = 0M;
                    entity.CalculateExtendedPrice();
                }

                if (string.IsNullOrEmpty(entity.CustomerPartNumber))
                {
                    EntityList<Alias> entityList = Alias.FindCustomerItemByAlias(CompId, entity.ItemId, CurrentTransaction.CustId);
                    entityList.Sort(AliasBase.Columns.AliasId.ToString());
                    if (entityList.Count > 0)
                    {
                        entity.CustomerPartNumber = entityList[0].AliasId;
                    }
                }
                return;
            }

            if (entity.SmItem != null)
                entity.SetDefaults();
        }

        protected virtual void ValidateQty(TransactionDetailLineItem entity)
        { }
        #endregion Line Item Update Methods

        #region Lot Update Methods
        protected virtual TransactionLot UpdateLot(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Sequence number is required.");

            this.FilterEntityList(parent.LotList, ApiArTransactionLotController.FunctionId);
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

        protected virtual void LotNumberPropertyChanged(TransactionLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum))
                return;

            entity.SetDefaults();

            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, false);

            if (entity.IsNew && ((TransactionHeader)entity.Parent).TransactionType == ARTransactionType.CreditMemo && lot == null)
            {
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }

            ValidateQty(entity);
        }

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionLot;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
        }

        protected virtual void ValidateQty(TransactionLot entity)
        { }
        #endregion Lot Update Methods

        #region Serial Update Methods
        protected virtual TransactionSerial UpdateSerial(TransactionDetailLineItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            this.FilterEntityList(parent.SerialList, ApiArTransactionSerialController.FunctionID);
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

        protected virtual void SerialNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum))
                return;

            var serial = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, false);

            if (serial == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' is not on file.", entity.SerNum));

            if (CurrentTransaction.TransactionType == ARTransactionType.Invoice)
            {
                if (serial.SerialItemStatus == SerialItemStatus.Available)
                {
                    serial.SerialItemStatus = SerialItemStatus.Sold;
                    entity.SetDefaults();
                }

                return;
            }

            if (serial.SerialItemStatus == SerialItemStatus.Sold)
            {
                serial.SerialItemStatus = SerialItemStatus.Available;
                entity.SetDefaults();
            }
            else
                throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Sold.", entity.SerNum));
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum))
                return;

            Lot lot = entity.InItem.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, false);

            if (entity.IsNew && ((TransactionHeader)entity.Parent.Parent).TransactionType == ARTransactionType.CreditMemo && lot == null)
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
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionDetailLineItem> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionDetailLineItem);
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionLot> action = null;
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionLot);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSerial> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected TransactionHeader CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransactionDetailLineItem>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionDetailLineItem>>();

        protected SortedDictionary<string, Action<TransactionLot>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLot>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "319578C4-E2B8-45A8-B03B-0D0416188764";
        #endregion Properties
    }
}
