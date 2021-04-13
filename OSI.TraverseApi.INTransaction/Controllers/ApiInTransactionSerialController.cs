#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.INTransaction;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.INTransaction
{
    public class ApiInTransactionSerialController : ApiControllerBase
    {
        #region Web Methods
        #region Adjustment Methods
        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> GetAdjustment(int transId, string id = null)
        {
            return Ok(await Load<TransactionAdjustment>(transId, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> PutAdjustment([FromBody] dynamic body, int transId, string id = null)
        {
            return Ok(await ProcessEditRequest<TransactionAdjustment>(false, body, transId, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> AddAdjustment([FromBody] dynamic body, int transId, string id = null)
        {
            return Ok(await ProcessEditRequest<TransactionAdjustment>(true, body, transId, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/serial/{id}", typeof(TransactionSerial))]
        public async Task DeleteAdjustment(int transId, string id)
        {
            await MarkToDelete<TransactionAdjustment>(transId, id);
        }
        #endregion Adjustment Methods

        #region Purchase Methods
        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> GetPurchase(int transId, string id = null)
        {
            return Ok(await Load<TransactionPurchase>(transId, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> PutPurchase([FromBody] dynamic body, int transId, string id = null)
        {
            return Ok(await ProcessEditRequest<TransactionPurchase>(false, body, transId, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> AddPurchase([FromBody] dynamic body, int transId, string id = null)
        {
            return Ok(await ProcessEditRequest<TransactionPurchase>(true, body, transId, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/serial/{id}", typeof(TransactionSerial))]
        public async Task DeletePurchase(int transId, string id)
        {
            await MarkToDelete<TransactionPurchase>(transId, id);
        }
        #endregion Purchase Methods

        #region Sale Methods
        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> GetSale(int transId, string id = null)
        {
            return Ok(await Load<TransactionSale>(transId, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> PutSale([FromBody] dynamic body, int transId, string id = null)
        {
            return Ok(await ProcessEditRequest<TransactionSale>(false, body, transId, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> AddSale([FromBody] dynamic body, int transId, string id = null)
        {
            return Ok(await ProcessEditRequest<TransactionSale>(true, body, transId, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/serial/{id}", typeof(TransactionSerial))]
        public async Task DeleteSale(int transId, string id)
        {
            await MarkToDelete<TransactionSale>(transId, id);
        }
        #endregion Sale Methods
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

        protected virtual async Task ApplyHeaderFilter<E>(IList list)
            where E : Transaction
        {
            if (typeof(TransactionAdjustment).IsAssignableFrom(typeof(E)))
                await this.FilterEntityListAsync(list, ApiInTransactionController.AdjustmentFunctionID);

            if (typeof(TransactionPurchase).IsAssignableFrom(typeof(E)))
                await this.FilterEntityListAsync(list, ApiInTransactionController.PurchaseFunctionID);

            if (typeof(TransactionSale).IsAssignableFrom(typeof(E)))
                await this.FilterEntityListAsync(list, ApiInTransactionController.SaleFunctionID);
        }

        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            PropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            EntityPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), ValidateCostUnitFgn);
            EntityPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), ValidateSerialLotNumber);
        }

        protected virtual bool GetOutboundStatus()
        {
            if (this.CurrentTransaction == null)
                return false;

            switch (CurrentTransaction.INTransactionType)
            {
                case INTransactionType.POMiscDebit:
                case INTransactionType.SOVerifyOrder:
                case INTransactionType.SOInvoice:
                case INTransactionType.Decrease:
                    return true;
            }
            return false;
        }

        protected virtual async Task<EntityList<TransactionSerial>> Load<E>(int transId, string id)
            where E : Transaction
        {
            var list = (LoadProvider<E>().ItemList as EntityList<E>).Find(x => x.TransId == transId)?.SerialList as EntityList<TransactionSerial>;

            if (CurrentTransaction == null || CurrentTransaction.TransId != transId)
            {
                var builder = new SqlFilterBuilder<TransactionBase.Columns>();
                builder.AppendEquals(TransactionBase.Columns.TransId, transId.ToString());
                builder.AppendRange(TransactionBase.Columns.TransType, TransTypeFrom, TransTypeThru);

                var provider = ObjectFactory.CreateObjectFactory(Provider.GetType())() as IProvider;
                provider.LoadList(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (provider.ItemList.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", transId));

                foreach (E item in provider.ItemList)
                    Provider.ItemList.Add(item);

                await ApplyHeaderFilter<E>(Provider.ItemList);

                CurrentTransaction = Provider.ItemList[0] as E;

                list = CurrentTransaction.SerialList as EntityList<TransactionSerial>;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(TransactionSerialBase.Columns.SerNum, id, true);

            return list;
        }

        protected virtual async Task<TransactionSerial> Find<E>(int transId, string id)
            where E : Transaction
        {
            var list = await Load<E>(transId, id);
            return list.Find(x => StringHelper.AreEqual(x.SerNum, id, false));
        }

        protected virtual async Task<List<TransactionSerial>> ProcessEditRequest<E>(bool isCreate, dynamic body, int transId, string id = null)
            where E : Transaction
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. Seq Num is provided along with more than one record.");

            var entityList = new List<TransactionSerial>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem<E>(isCreate, item, transId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            RecalculateTotals();
            
            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionSerial> ProcessBodyItem<E>(bool isCreate, dynamic bodyItem, int transId, string id)
            where E : Transaction
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum) || string.IsNullOrWhiteSpace(bodyItem.SerNum))
                bodyItem.SerNum = code;
            else
                code = bodyItem.SerNum;

            var entity = await this.Find<E>(transId, code);

            if (CurrentTransaction.ItemInfo == null || CurrentTransaction.ItemInfo.InventoryType != InventoryType.Serial)
                throw new InvalidValueException("This item is not a serialized item and cannot be processed.");

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = (CurrentTransaction.SerialList as EntityList<TransactionSerial>).AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Serial Num {0} could not be found on transaction '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete<E>(int transId, string id)
            where E : Transaction
        {
            var entity = await this.Find<E>(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Serial Num {0} could not be found on transaction '{1}'", id, transId));

            CurrentTransaction.SerialList.Remove(entity);
            RecalculateTotals();
            this.Provider.Update(this.CompId);
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

        protected virtual void SerialNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || CurrentTransaction.ItemInfo == null
                || CurrentTransaction.ItemInfo.InventoryType != InventoryType.Serial
                || CurrentTransaction.INTransactionType == INTransactionType.SONewOrder
                || CurrentTransaction.INTransactionType == INTransactionType.PONewOrder)
                return;

            var serial = CurrentTransaction.ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .SerialItems.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            //Default the lot number for existing serial numbers
            if (serial != null && !string.IsNullOrEmpty(serial.LotNum))
                entity.LotNum = serial.LotNum;

            if (IsOutboundTransaction)
            {
                if (serial == null)
                    throw new InvalidValueException(string.Format("Serial Number '{0}' is not on file.", entity.SerNum));

                if (serial.SerialItemStatus == SerialItemStatus.Available)
                {
                    serial.SerialItemStatus = CurrentTransaction.TransactionType == TransactionType.Decrease ? SerialItemStatus.Lost : SerialItemStatus.Sold;
                    entity.SetPriceCostDefault();
                }
                else
                    throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Available.", entity.SerNum));

                return;
            }

            if (serial != null && (CurrentTransaction.TransactionType == TransactionType.InvoicePurchaseIN || CurrentTransaction.TransactionType == TransactionType.InvoiceAP))
                throw new InvalidValueException(string.Format("Serial Number '{0}' already exists.", entity.SerNum));

            if (serial != null
                && (CurrentTransaction.TransactionType == TransactionType.Increase && serial?.SerialItemStatus == SerialItemStatus.Lost) ||
                (CurrentTransaction.TransactionType != TransactionType.Increase && serial?.SerialItemStatus == SerialItemStatus.Sold))
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
                    if (CurrentTransaction.TransactionType == TransactionType.Increase && serial?.SerialItemStatus != SerialItemStatus.Lost)
                        throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Lost to be adjusted.", entity.SerNum));

                    //SO-PO
                    if (CurrentTransaction.TransactionType != TransactionType.Increase && serial?.SerialItemStatus != SerialItemStatus.Sold)
                        throw new InvalidValueException(string.Format("Serial Number '{0}' must have a status of Sold.", entity.SerNum));
                }
            }
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || CurrentTransaction.ItemInfo == null
                || !CurrentTransaction.ItemInfo.IsLotted
                || CurrentTransaction.INTransactionType == INTransactionType.SONewOrder
                || CurrentTransaction.INTransactionType == INTransactionType.PONewOrder)
                return;

            Lot lot = CurrentTransaction.ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (this.IsOutboundTransaction)
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

        protected virtual void RecalculateTotals()
        {
            CurrentTransaction.Calculate();
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
            Action<TransactionSerial> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected IProvider Provider { get; private set; }

        protected Transaction CurrentTransaction { get; set; }

        protected bool IsOutboundTransaction => GetOutboundStatus();

        protected SortedDictionary<string, Action<TransactionSerial>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private string TransTypeFrom { get; set; }

        private string TransTypeThru { get; set; }
        #endregion Properties

        #region Fields
        public const string AdjustmentFunctionID = "45e50ffe-fe7e-44af-a266-d4396df42828";
        public const string SaleFunctionID = "A20AFB9D-6BC4-4A1E-AA9D-E5FFB97614DB";
        public const string PurchaseFunctionID = "9421B02E-F63D-4B30-B3BA-D39C21FEC275";
        #endregion Fields
    }
}
