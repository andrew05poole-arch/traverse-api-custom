#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Batching;
using TRAVERSE.Business.INTransaction;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.INTransaction
{
    public class ApiInTransactionController : ApiControllerBase
    {
        #region Web Methods
        #region Adjustment Requests
        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{id:int?}", typeof(TransactionAdjustment), new object[] { ApiInTransactionSerialController.AdjustmentFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> GetAdjustment(int? id = null)
        {
            return Ok(await Load<TransactionAdjustment>(id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{id:int?}", typeof(TransactionAdjustment), new object[] { ApiInTransactionSerialController.AdjustmentFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> PutAdjustment([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionAdjustment>(false, body, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{id:int?}", typeof(TransactionAdjustment), new object[] { ApiInTransactionSerialController.AdjustmentFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> AddAdjustment([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionAdjustment>(true, body, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{id:int}", typeof(TransactionAdjustment), new object[] { ApiInTransactionSerialController.AdjustmentFunctionID, typeof(TransactionSerial) })]
        public async Task DeleteAdjustment(int id)
        {
            await this.MarkToDelete<TransactionAdjustment>(id);
        }
        #endregion  Adjustment Requests

        #region Purchase Requests
        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{id:int?}", typeof(TransactionPurchase), new object[] { ApiInTransactionSerialController.PurchaseFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> GetPurchase(int? id = null)
        {
            return Ok(await Load<TransactionPurchase>(id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{id:int?}", typeof(TransactionPurchase), new object[] { ApiInTransactionSerialController.PurchaseFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> PutPurchase([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionPurchase>(false, body, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{id:int?}", typeof(TransactionPurchase), new object[] { ApiInTransactionSerialController.PurchaseFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> AddPurchase([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionPurchase>(true, body, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{id:int}", typeof(TransactionPurchase), new object[] { ApiInTransactionSerialController.PurchaseFunctionID, typeof(TransactionSerial) })]
        public async Task DeletePurchase(int id)
        {
            await this.MarkToDelete<TransactionPurchase>(id);
        }
        #endregion  Purchase Requests

        #region Sale Requests
        [ApiRoute(SaleFunctionID, 2f, "sale/{id:int?}", typeof(TransactionSale), new object[] { ApiInTransactionSerialController.SaleFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> GetSale(int? id = null)
        {
            return Ok(await Load<TransactionSale>(id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{id:int?}", typeof(TransactionSale), new object[] { ApiInTransactionSerialController.SaleFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> PutSale([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionSale>(false, body, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{id:int?}", typeof(TransactionSale), new object[] { ApiInTransactionSerialController.SaleFunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> AddSale([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionSale>(true, body, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{id:int}", typeof(TransactionSale), new object[] { ApiInTransactionSerialController.SaleFunctionID, typeof(TransactionSerial) })]
        public async Task DeleteSale(int id)
        {
            await this.MarkToDelete<TransactionSale>(id);
        }
        #endregion  Sale Requests
        #endregion Web Methods

        #region Helper Methods
        private IProvider LoadProvider<E>()
            where E : IEntity
        {
            if (Provider == null)
                Provider = CreateProvider(typeof(E));

            return Provider;
        }

        protected virtual IProvider CreateProvider(Type type)
        {
            switch (type.Name)
            {
                case "TransactionAdjustment":
                    TransTypeFrom = "31";
                    TransTypeThru = "32";
                    return new TransactionAdjustmentProvider();
                case "TransactionPurchase":
                    TransTypeFrom = "11";
                    TransTypeThru = "15";
                    return new TransactionPurchaseProvider();
                case "TransactionSale":
                    TransTypeFrom = "21";
                    TransTypeThru = "25";
                    return new TransactionSaleProvider();
                default:
                    return null;
            }
        }

        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(TransactionBase.Columns.Qty.ToString(), QtyPropertyChanged);
            PropertyDictionary.Add(TransactionBase.Columns.ItemId.ToString(), ItemIdPropertyChanged);
            PropertyDictionary.Add(TransactionBase.Columns.TransDate.ToString(), TransDatePropertyChanged);
            PropertyDictionary.Add(TransactionBase.Columns.Uom.ToString(), (entity) => SetPriceCost(entity));

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionLotBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            EntityPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), ValidateCostUnitFgn);
            EntityPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), ValidateSerialLotNumber);
        }

        protected virtual void ApplyHeaderFilter(Type parentType, IList list)
        {
            if (typeof(TransactionAdjustment).IsAssignableFrom(parentType))
                this.FilterEntityList(list, ApiInTransactionController.AdjustmentFunctionID);

            if (typeof(TransactionPurchase).IsAssignableFrom(parentType))
                this.FilterEntityList(list, ApiInTransactionController.PurchaseFunctionID);

            if (typeof(TransactionSale).IsAssignableFrom(parentType))
                this.FilterEntityList(list, ApiInTransactionController.SaleFunctionID);
        }

        protected virtual async Task<EntityList<E>> Load<E>(int? id)
            where E : Transaction
        {
            var list = LoadProvider<E>().ItemList as EntityList<E>;
            if (list.Count <= 0 || (id.HasValue && !list.Exists(i => id == i.TransId)))
            {
                var builder = new SqlFilterBuilder<TransactionBase.Columns>();
                builder.AppendRange(TransactionBase.Columns.TransType, TransTypeFrom, TransTypeThru);

                if (!id.HasValue)
                    await Provider.Load<E>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty),
                        PageNumber, PageSize);
                else
                {
                    builder.AppendEquals(TransactionBase.Columns.TransId, id.ToString());

                    var provider = ObjectFactory.CreateObjectFactory(Provider.GetType())() as IProvider;
                    provider.LoadList(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                    foreach (E item in provider.ItemList)
                        Provider.ItemList.Add(item);
                }
                await this.FilterEntityListAsync(Provider.ItemList);
            }
            return Provider.ItemList as EntityList<E>;
        }

        protected virtual async Task<E> Find<E>(int id)
            where E : Transaction
        {
            var list = await Load<E>(id);
            return list.Find(x => x.TransId == id);
        }

        protected virtual async Task<List<E>> ProcessEditRequest<E>(bool isCreate, dynamic body, int? id = null)
            where E : Transaction
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Transaction is provided along with more than one record.");

            var entityList = new List<E>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem<E>(isCreate, item, id);
                ((EntityList<E>)Provider.ItemList).Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<E> ProcessBodyItem<E>(bool isCreate, dynamic bodyItem, int? id)
            where E : Transaction
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || bodyItem.TransId == null)
                bodyItem.TransId = code;
            else
                code = Convert.ToInt32(bodyItem.TransId);

            var entity = await this.Find<E>(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = Activator.CreateInstance(typeof(E), new object[] { this.CompId }) as E;
                entity.BatchId = Batch.GetDefaultBatchId(Utility.FunctionIdTransaction, Utility.UseBatch, this.CompId, null);
                entity.SetDefaults();
                entity.SetGLPeriodYearFromDate(DateTime.Today);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            if (entity.INTransactionType != INTransactionType.SONewOrder && entity.INTransactionType != INTransactionType.PONewOrder)
            {
                if (entity.ItemInfo.InventoryType == InventoryType.Serial && entity.SerialList?.Count <= 0)
                    throw new InvalidValueException("Serial list is required.");
                else if (entity.ItemInfo.InventoryType != InventoryType.Serial && entity.ItemInfo.IsLotted && entity.LotList?.Count <= 0)
                    throw new InvalidValueException("Lot list is required.");
            }

            return entity;
        }

        protected virtual async Task MarkToDelete<E>(int id)
            where E : Transaction
        {
            var entity = await this.Find<E>(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transaction '{0}' could not be found.", id));

            this.Provider.ItemList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            var transaction = (Transaction)args.ParentObject;

            if (args.PropertyName == "LotList")
            {
                if (transaction.ItemInfo == null || !transaction.ItemInfo.IsLotted || transaction.ItemInfo.InventoryType != InventoryType.Regular)
                    throw new InvalidValueException("This item is not a regular lotted item and cannot be processed");

                if (transaction.IsNew)
                    return this.CreateLot(transaction, args.ItemModel);
                else
                    return this.UpdateLot(transaction, args.ItemModel);
            }

            if (args.PropertyName == "SerialList")
            {
                if (transaction.ItemInfo == null || transaction.ItemInfo.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("This item is not a serialized item and cannot be processed");

                if (transaction.IsNew)
                    return this.CreateSerial(transaction, args.ItemModel);
                else
                    return this.UpdateSerial(transaction, args.ItemModel);
            }

            return null;
        }

        protected virtual TransactionLot UpdateLot(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Sequence number is required.");

            TransactionLot entity = parent.LotList.Find(x => x.SeqNum == (int)bodyItem.SeqNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Lot '{0}' for Transaction ID '{1}' could not be found.", bodyItem.SeqNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionLot CreateLot(Transaction parent, dynamic bodyItem)
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

        protected virtual TransactionSerial UpdateSerial(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Transaction ID '{1}' could not be found.",
                    bodyItem.SerNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionSerial CreateSerial(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Transaction ID '{1}' already exists.",
                    bodyItem.SerNum, parent.TransId));

            entity = (parent.SerialList as EntityList<TransactionSerial>).AddNew();

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

        protected virtual bool IsOutboundTransaction(Transaction transaction)
        {
            if (transaction == null)
                return false;

            switch (transaction.INTransactionType)
            {
                case INTransactionType.POMiscDebit:
                case INTransactionType.SOVerifyOrder:
                case INTransactionType.SOInvoice:
                case INTransactionType.Decrease:
                    return true;
            }
            return false;
        }

        #region Header Update Methods
        protected virtual void ItemIdPropertyChanged(Transaction entity)
        {
            if (entity.ItemInfo != null && entity.ItemInfo.InventoryStatus == InventoryStatus.Superseded)
                entity.ItemId = entity.ItemInfo.SuperId;

            if (entity.ItemInfo == null)
            {
                entity.LocId = null;
                entity.Uom = null;
                return;
            }

            if (entity.ItemInfo.AllLocations.Count < 1)
                throw new InvalidValueException(string.Format("Item {0} has no locations.", entity.ItemId));

            SetItemDefault(entity);
            SetPriceCost(entity);
        }

        protected virtual void QtyPropertyChanged(Transaction entity)
        {
            SetItemDefault(entity);
            SetPriceCost(entity);
        }

        protected virtual void TransDatePropertyChanged(Transaction entity)
        {
            entity.SetGLPeriodYearFromDate(entity.TransDate.GetValueOrDefault(DateTime.Today));
            if (entity.IsNew)
                entity.SetExhangeRateTransDefaultList();
        }

        protected virtual void SetItemDefault(Transaction entity)
        {
            if (typeof(TransactionAdjustment).IsAssignableFrom(entity.GetType()))
                ((TransactionAdjustment)entity).SetItemDefault();

            if (typeof(TransactionPurchase).IsAssignableFrom(entity.GetType()))
                ((TransactionPurchase)entity).SetItemDefault();

            if (typeof(TransactionSale).IsAssignableFrom(entity.GetType()))
                ((TransactionSale)entity).SetItemDefault();
        }

        protected virtual void SetPriceCost(Transaction entity)
        {
            if (typeof(TransactionAdjustment).IsAssignableFrom(entity.GetType()))
                ((TransactionAdjustment)entity).SetCost();

            if (typeof(TransactionPurchase).IsAssignableFrom(entity.GetType()))
                ((TransactionPurchase)entity).SetCostDefault();

            if (typeof(TransactionSale).IsAssignableFrom(entity.GetType()))
                ((TransactionSale)entity).SetPriceCostDefault();
        }
        #endregion Header Update Methods

        #region Lot Update Methods
        protected virtual void LotNumberPropertyChanged(TransactionLot entity)
        {
            var transaction = entity.Parent as Transaction;
            if (string.IsNullOrEmpty(entity.LotNum)
                || transaction.ItemInfo == null
                || !transaction.ItemInfo.IsLotted
                || transaction.INTransactionType == INTransactionType.SONewOrder
                || transaction.INTransactionType == INTransactionType.PONewOrder)
                return;

            Lot lot = transaction.ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (IsOutboundTransaction(transaction))
            {
                if (lot == null)
                    throw new InvalidValueException(string.Format("Lot Number '{0}' is not on file.", entity.LotNum));
            }
            else
            {
                if (lot == null)
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
            var transaction = entity.Parent as Transaction;
            if (string.IsNullOrEmpty(entity.SerNum)
                || transaction.ItemInfo == null
                || transaction.ItemInfo.InventoryType != InventoryType.Serial
                || transaction.INTransactionType == INTransactionType.SONewOrder
                || transaction.INTransactionType == INTransactionType.PONewOrder)
                return;

            var serial = transaction.ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            //Default the lot number for existing serial numbers
            if (serial != null && !string.IsNullOrEmpty(serial.LotNum))
                entity.LotNum = serial.LotNum;

            if (IsOutboundTransaction(transaction))
            {
                if (serial == null)
                    throw new InvalidValueException(string.Format("Serial Number '{0}' is not on file.", entity.SerNum));

                if (serial.SerialItemStatus == SerialItemStatus.Available)
                {
                    serial.SerialItemStatus = transaction.TransactionType == TransactionType.Decrease
                                                ? SerialItemStatus.Lost : SerialItemStatus.Sold;
                    entity.SetPriceCostDefault();
                }
                else
                    throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Available.", entity.SerNum));

                return;
            }

            if (serial != null 
                && (transaction.TransactionType == TransactionType.InvoicePurchaseIN 
                || transaction.TransactionType == TransactionType.InvoiceAP))
                throw new InvalidValueException(string.Format("Serial Number '{0}' already exists.", entity.SerNum));

            if (serial != null &&
                (transaction.TransactionType == TransactionType.Increase && serial?.SerialItemStatus == SerialItemStatus.Lost) ||
                (transaction.TransactionType != TransactionType.Increase && serial?.SerialItemStatus == SerialItemStatus.Sold))
            {
                serial.SerialItemStatus = SerialItemStatus.Available;
                entity.SetPriceCostDefault();
            }
            else
            {
                //Existing serial item was selected but it has a wrong status
                if (serial != null)
                {
                    //Adjustments
                    if (transaction.TransactionType == TransactionType.Increase && serial?.SerialItemStatus != SerialItemStatus.Lost)
                        throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Lost to be adjusted.", entity.SerNum));

                    //SO-PO
                    if (transaction.TransactionType != TransactionType.Increase && serial?.SerialItemStatus != SerialItemStatus.Sold)
                        throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Sold.", entity.SerNum));
                }             
            }
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            var transaction = entity.Parent as Transaction;

            if (string.IsNullOrEmpty(entity.LotNum)
                || transaction.ItemInfo == null
                || !transaction.ItemInfo.IsLotted
                || transaction.INTransactionType == INTransactionType.SONewOrder
                || transaction.INTransactionType == INTransactionType.PONewOrder)
                return;

            Lot lot = transaction.ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (IsOutboundTransaction(transaction))
            {
                if (lot == null)
                    throw new InvalidValueException(string.Format("Lot Number '{0}' is not on file.", entity.LotNum));
            }
            else
            {
                if (lot == null)
                    entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }
        }

        protected virtual void ValidateCostUnitFgn(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionSerial serialInfo)
                {
                    if (serialInfo.Parent.TransactionType == TransactionType.Decrease
                        || ((serialInfo.Parent as Transaction).INTransactionType == INTransactionType.SOVerifyOrder || (serialInfo.Parent as Transaction).INTransactionType == INTransactionType.SOInvoice)
                        || (serialInfo.Parent as Transaction).INTransactionType == INTransactionType.POMiscDebit)
                        e.Handled = true;
                }
                else
                {
                    if (e.Entity is TransactionLot lotInfo)
                        if ((lotInfo.Parent as Transaction).TransactionType == TransactionType.Decrease
                            || ((lotInfo.Parent as Transaction).INTransactionType == INTransactionType.SOVerifyOrder || (lotInfo.Parent as Transaction).INTransactionType == INTransactionType.SOInvoice)
                            || (lotInfo.Parent as Transaction).INTransactionType == INTransactionType.POMiscDebit)
                            e.Handled = true;
                }
            }
        }

        protected virtual void ValidateSerialLotNumber(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionSerial serialInfo)
                {
                    if (serialInfo.Parent.TransactionType == TransactionType.Decrease
                        || ((serialInfo.Parent as Transaction).INTransactionType == INTransactionType.SOVerifyOrder || (serialInfo.Parent as Transaction).INTransactionType == INTransactionType.SOInvoice)
                        || (serialInfo.Parent as Transaction).INTransactionType == INTransactionType.POMiscDebit)
                    {
                        e.Handled = true;
                    }
                    else if (serialInfo.Parent.TransactionType == TransactionType.Increase
                              || (serialInfo.Parent as Transaction).INTransactionType == INTransactionType.SOMiscCredit
                              || ((serialInfo.Parent as Transaction).INTransactionType == INTransactionType.POGoodsRcvd || (serialInfo.Parent as Transaction).INTransactionType == INTransactionType.POInvoice)
                              && !string.IsNullOrEmpty(serialInfo.SerNum))
                    {
                        Transaction transaction = (Transaction)serialInfo.Parent;

                        if (transaction != null)
                        {
                            var serial = transaction.ItemInfo?.AllLocations?.Find(ItemLocationBase.Columns.LocId, transaction.LocId, true)?
                                    .SerialItems?.Find(SerialItemBase.Columns.SerNum, serialInfo.SerNum, true);

                            if (serial != null)
                                e.Handled = true;
                        }
                    }
                }
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
            Action<Transaction> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Transaction);
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
        protected IProvider Provider { get; private set; }

        protected SortedDictionary<string, Action<Transaction>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Transaction>>();

        protected SortedDictionary<string, Action<TransactionLot>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLot>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private string TransTypeFrom { get; set; }

        private string TransTypeThru { get; set; }
        #endregion Properties

        #region Fields
        public const string AdjustmentFunctionID = "53955C2F-A1A0-4F9B-9D27-2ED2161F8CD3";
        public const string SaleFunctionID = "CCC87E99-9F77-4FDE-BAFE-E236C7D8AA64";
        public const string PurchaseFunctionID = "EC7C5F49-EEEF-4B4C-909D-24E8E723CA7D";
        #endregion Fields
    }
}
