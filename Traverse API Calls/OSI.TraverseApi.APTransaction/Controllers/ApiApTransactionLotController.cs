#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class ApiApTransactionLotController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionId, 2f, "transaction/{transid}/lineitem/{entrynum:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> Get(string transId, int entryNum, int? id = null)
        {           
            return Ok(await Load(transId, entryNum, id));
        }

        [ApiRoute(FunctionId, 2f, "transaction/{transid}/lineitem/{entrynum:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId, int entryNum, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, entryNum, id));
        }

        [ApiRoute(FunctionId, 2f, "transaction/{transid}/lineitem/{entrynum:int}/lot/{id:int?}", typeof(TransactionLot))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, int entryNum, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, entryNum, id));
        }

        [ApiRoute(FunctionId, 2f, "transaction/{transid}/lineitem/{entrynum:int}/lot/{id:int}", typeof(TransactionLot))]
        public async Task Delete(string transId, int entryNum, int id)
        {
            await MarkToDelete(transId, entryNum, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransactionLotBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            PropertyDictionary.Add(TransactionLotBase.Columns.CostUnitFgn.ToString(), (entity) => entity.CalculateBaseCost());
        }

        protected virtual async Task Load(string transId, int id)
        {
            var list = CurrentTransaction?.DetailList as EntityList<TransactionDetailLineItem>;

            if (CurrentTransaction == null || !StringHelper.AreEqual(CurrentTransaction.TransId, transId, false))
            {
                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiApTransactionOrderController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", transId));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.DetailList as EntityList<TransactionDetailLineItem>;
                await this.FilterEntityListAsync(list, ApiApTransactionDetailController.FunctionId);
            }

            CurrentLineItem = list.Find(TransactionDetailBase.Columns.EntryNum, id);
            if (CurrentLineItem == null)
                throw new InvalidValueException(string.Format("Line Item {0} could not be found on transaction {1}", id, transId));
        }

        protected virtual async Task<EntityList<TransactionLot>> Load(string transId, int entryNum, int? id)
        {
            var list = CurrentLineItem?.LotList;

            if (CurrentLineItem == null || !StringHelper.AreEqual(CurrentLineItem.TransId, transId, false) || CurrentLineItem.EntryNum != entryNum)
            {
                await Load(transId, entryNum);
                
                list = CurrentLineItem.LotList;
                await this.FilterEntityListAsync(list);
            }                

            if (id.HasValue)
                list = list?.FindAll(TransactionLotBase.Columns.SeqNum, id.Value);

            return list;
        }

        protected virtual async Task<TransactionLot> Find(string transId, int entryNum, int id)
        {
            var list = await Load(transId, entryNum, id);
            return list?.Find(x => x.SeqNum == id);
        }

        protected virtual async Task<List<TransactionLot>> ProcessEditRequest(bool isCreate, dynamic body, string transId, int entryNum, int? id = null)
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
                var entity = await this.ProcessBodyItem(isCreate, item, transId, entryNum, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            RecalculateTotals();
            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionLot> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, int entryNum, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum) || bodyItem.SeqNum == null)
                bodyItem.SeqNum = code;
            else
                code = Convert.ToInt32(bodyItem.SeqNum);

            var entity = await this.Find(transId, entryNum, code);

            if (CurrentLineItem != null)
            {
                if (CurrentLineItem.INItemInfo == null || !CurrentLineItem.INItemInfo.IsLotted || CurrentLineItem.INItemInfo.InventoryType != InventoryType.Regular)
                    throw new InvalidValueException("This item is not a regular lotted item and cannot be processed.");
            }

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentLineItem.LotList.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Seq Num {0} could not be found on line item {1} on transaction '{2}'.", code, entryNum, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, int entryNum, int id)
        {
            var entity = await this.Find(transId, entryNum, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Seq Num {0} could not be found on line item {1} on transaction '{2}'", id, entryNum, transId));

            CurrentLineItem.LotList.Remove(entity);
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
            if (string.IsNullOrEmpty(entity.LotNum))
                return;

            entity.SetDefaults();
            entity.SetCostDefault();

            Lot lot = entity.INItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && ((TransactionHeader)CurrentLineItem.Parent).TransactionType == APTransactionType.Invoice && lot == null)
            {
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }
        }

        protected virtual void RecalculateTotals()
        {
            TransactionHeader.Recalculate(CurrentTransaction);
            CurrentTransaction.CalculateTotals();

            if (ConfigurationValueProvider.GetRule<bool>(AppId.AP, ConfigurationValue.DiscAutoYn, this.CompId))
                CurrentTransaction.CashDiscFgn = CurrentTransaction.GetDiscountAmount();

            CurrentTransaction.SetPayments(0);
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
        protected TransactionHeaderProvider Provider { get; } = new TransactionHeaderProvider();

        protected TransactionHeader CurrentTransaction { get; set; }

        protected TransactionDetailLineItem CurrentLineItem { get; set; }

        protected SortedDictionary<string, Action<TransactionLot>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLot>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionId = "F76289DB-00E0-483A-A5D6-6FBABB9ABC7B";
        #endregion Properties
    }
}
