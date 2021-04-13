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
    public class ApiInTransactionLotController : ApiControllerBase
    {
        #region Web Methods
        #region Adjustment Methods
        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> GetAdjustment(int transId, int? id = null)
        {
            return Ok(await Load<TransactionAdjustment>(transId, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> PutAdjustment([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionAdjustment>(false, body, transId, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> AddAdjustment([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionAdjustment>(true, body, transId, id));
        }

        [ApiRoute(AdjustmentFunctionID, 2f, "adjustment/{transid:int}/lot/{id:int}", typeof(TransactionLot))]
        public async Task DeleteAdjustment(int transId, int id)
        {
            await MarkToDelete<TransactionAdjustment>(transId, id);
        }
        #endregion Adjustment Methods

        #region Purchase Methods
        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> GetPurchase(int transId, int? id = null)
        {
            return Ok(await Load<TransactionPurchase>(transId, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> PutPurchase([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionPurchase>(false, body, transId, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> AddPurchase([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionPurchase>(true, body, transId, id));
        }

        [ApiRoute(PurchaseFunctionID, 2f, "purchase/{transid:int}/lot/{id:int}", typeof(TransactionLot))]
        public async Task DeletePurchase(int transId, int id)
        {
            await MarkToDelete<TransactionPurchase>(transId, id);
        }
        #endregion Purchase Methods

        #region Sale Methods
        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> GetSale(int transId, int? id = null)
        {
            return Ok(await Load<TransactionSale>(transId, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> PutSale([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionSale>(false, body, transId, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> AddSale([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest<TransactionSale>(true, body, transId, id));
        }

        [ApiRoute(SaleFunctionID, 2f, "sale/{transid:int}/lot/{id:int}", typeof(TransactionLot))]
        public async Task DeleteSale(int transId, int id)
        {
            await MarkToDelete<TransactionSale>(transId, id);
        }
        #endregion Adjustment Methods
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
            PropertyDictionary.Add(TransactionLotBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            EntityPropertyDictionary.Add(TransactionSerialBase.Columns.CostUnitFgn.ToString(), ValidateCostUnitFgn);
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

        protected virtual async Task<EntityList<TransactionLot>> Load<E>(int transId, int? id)
            where E : Transaction
        {
            var list = (LoadProvider<E>().ItemList as EntityList<E>).Find(x => x.TransId == transId)?.LotList;

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

                list = CurrentTransaction.LotList;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(TransactionLotBase.Columns.SeqNum, id.Value);

            return list;
        }

        protected virtual async Task<TransactionLot> Find<E>(int transId, int id)
            where E : Transaction
        {
            var list = await Load<E>(transId, id);
            return list.Find(x => x.SeqNum == id);
        }

        protected virtual async Task<List<TransactionLot>> ProcessEditRequest<E>(bool isCreate, dynamic body, int transId, int? id = null)
            where E : Transaction
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Seq Num is provided along with more than one record.");

            var entityList = new List<TransactionLot>();
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

        protected virtual async Task<TransactionLot> ProcessBodyItem<E>(bool isCreate, dynamic bodyItem, int transId, int? id)
            where E : Transaction
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum) || bodyItem.SeqNum == null)
                bodyItem.SeqNum = code;
            else
                code = Convert.ToInt32(bodyItem.SeqNum);

            var entity = await this.Find<E>(transId, code);

            if (CurrentTransaction.ItemInfo == null || !CurrentTransaction.ItemInfo.IsLotted || CurrentTransaction.ItemInfo.InventoryType != InventoryType.Regular)
                throw new InvalidValueException("This item is not a regular lotted item and cannot be processed.");

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentTransaction.LotList.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Seq Num {0} could not be found on transaction '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete<E>(int transId, int id)
            where E : Transaction
        {
            var entity = await this.Find<E>(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Seq Num {0} could not be found on transaction '{1}'", id, transId));

            CurrentTransaction.LotList.Remove(entity);
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

        protected virtual void LotNumberPropertyChanged(TransactionLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum) 
                || CurrentTransaction.ItemInfo == null 
                || !CurrentTransaction.ItemInfo.IsLotted
                || CurrentTransaction.INTransactionType == INTransactionType.SONewOrder 
                || CurrentTransaction.INTransactionType == INTransactionType.PONewOrder)
                return;

            entity.SetDefaults();
            entity.SetCostDefault();

            Lot lot = ((Transaction)entity.Parent).ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (IsOutboundTransaction)
            {
                if (lot == null)
                    throw new InvalidValueException(string.Format("Lot Number '{0}' is not on file.", entity.LotNum));
            }
            else
            {
                if (lot == null)
                    entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }
            ValidateQty(entity);
        }

        protected virtual void ValidateQty(TransactionLot entity)
        { }

        protected virtual void ValidateCostUnitFgn(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionLot lotInfo)
                    if ((lotInfo.Parent as Transaction).TransactionType == TransactionType.Decrease
                        || ((lotInfo.Parent as Transaction).INTransactionType == INTransactionType.SOVerifyOrder || (lotInfo.Parent as Transaction).INTransactionType == INTransactionType.SOInvoice)
                        || (lotInfo.Parent as Transaction).INTransactionType == INTransactionType.POMiscDebit)
                        e.Handled = true;
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
            Action<TransactionLot> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionLot);
        }
        #endregion Event Handlers

        #region Properties
        protected IProvider Provider { get; private set; }

        protected Transaction CurrentTransaction { get; set; }

        protected bool IsOutboundTransaction => GetOutboundStatus();

        protected SortedDictionary<string, Action<TransactionLot>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLot>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private string TransTypeFrom { get; set; }

        private string TransTypeThru { get; set; }
        #endregion Properties

        #region Fields
        public const string AdjustmentFunctionID = "5dbecf27-72cd-44ca-a125-d622849910bc";
        public const string SaleFunctionID = "12cd7423-946c-4192-8315-632fb1d761e3";
        public const string PurchaseFunctionID = "5ba5c0a5-f881-4270-9846-04e4f9030351";
        #endregion Fields
    }
}
