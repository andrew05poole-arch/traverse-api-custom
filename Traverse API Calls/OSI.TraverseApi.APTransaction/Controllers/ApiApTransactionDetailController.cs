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
#endregion Using Directives

namespace TRAVERSE.Web.API.AccountsPayable.Controllers
{
    public class ApiApTransactionDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionId, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Get(string transId, int? id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionId, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionId, 2f, "transaction/{transId}/lineitem/{id:int?}", typeof(TransactionDetailLineItem), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionId, 2f, "transaction/{transId}/lineitem/{id:int}", typeof(TransactionDetailLineItem), new object[] { ApiApTransactionSerialController.FunctionId, typeof(TransactionSerial) })]
        public async Task Delete(string transId, int id)
        {
            await this.MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Line Item Property Changes
            PropertyDictionary.Add(TransactionDetailBase.Columns.WhseId.ToString(), WhsePropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.Qty.ToString(), QtyPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.UnitCostFgn.ToString(), UnitCostPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.ExtCostFgn.ToString(), ExtCostPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.Units.ToString(), UnitPropertyChanged);
            PropertyDictionary.Add(TransactionDetailBase.Columns.PartId.ToString(), ItemIdPropertyChanged);

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionLotBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionLotBase.Columns.CostUnitFgn.ToString(), (entity) => entity.CalculateBaseCost());

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), (entity) => entity.CalculateBaseCost());
        }

        protected virtual async Task<EntityList<TransactionDetailLineItem>> Load(string transId, int? id)
        {
            var list = CurrentTransaction?.DetailList as EntityList<TransactionDetailLineItem>;

            if (CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.TransId, transId, false))
            {
                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiApTransactionOrderController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", transId));

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

            RecalculateTotals();
            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionDetailLineItem> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, int? id)
        {
            int code = id.GetValueOrDefault();
            int eNumber = 0;

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
                entity.Quantity = 1m;
                eNumber = entity.EntryNum;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Line Item {0} not be found on transaction '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            if(isCreate)
            {
                entity.EntryNum = eNumber;
            }
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
            RecalculateTotals();
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

        protected virtual void RecalculateTotals()
        {
            TransactionHeader.Recalculate(CurrentTransaction);
            CurrentTransaction.CalculateTotals();

            if (ConfigurationValueProvider.GetRule<bool>(AppId.AP, ConfigurationValue.DiscAutoYn, this.CompId))
                CurrentTransaction.CashDiscFgn = CurrentTransaction.GetDiscountAmount();

            CurrentTransaction.SetPayments(0);
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

        protected virtual void SerialNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum))
                return;

            var serial = entity.INItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            if (((TransactionHeader)entity.Parent).TransactionType == APTransactionType.Invoice)
            {
                if (serial != null)
                    throw new InvalidValueException(string.Format("Serial Number '{0}' already exists for this item.", entity.SerNum));

                serial.SerialItemStatus = SerialItemStatus.Available;
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

            if (entity.IsNew && ((TransactionHeader)entity.Parent).TransactionType == APTransactionType.Invoice && lot == null)
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

        public const string FunctionId = "5D148E4A-88B7-464B-9481-F4ECAC0BE852";
        #endregion Properties
    }
}
