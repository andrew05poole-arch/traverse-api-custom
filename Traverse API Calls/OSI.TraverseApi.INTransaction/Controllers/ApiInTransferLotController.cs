#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.INTransaction;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.INTransaction
{
    public class ApiInTransferLotController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionId, 2f, "transfer/{transid:int}/lot/{id:int?}", typeof(TransferLot))]
        public async Task<IHttpActionResult> Get(int transId, int? id = null)
        {
            return Ok(await Load(transId, id));
        }

        [ApiRoute(FunctionId, 2f, "transfer/{transid:int}/lot/{id:int?}", typeof(TransferLot))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, id));
        }

        [ApiRoute(FunctionId, 2f, "transfer/{transid:int}/lot/{id:int?}", typeof(TransferLot))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int transId, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, id));
        }

        [ApiRoute(FunctionId, 2f, "transfer/{transid:int}/lot/{id:int}", typeof(TransferLot))]
        public async Task Delete(int transId, int id)
        {
            await MarkToDelete(transId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransferLotBase.Columns.LotNumFrom.ToString(), LotNumberFromPropertyChanged);
            PropertyDictionary.Add(TransferLotBase.Columns.LotNumTo.ToString(), LotNumberToPropertyChanged);
        }

        protected virtual async Task<EntityList<TransferLot>> Load(int transId, int? id)
        {
            var list = CurrentTransaction?.LotList;

            if (CurrentTransaction == null || CurrentTransaction.TransId != transId)
            {
                var builder = new SqlFilterBuilder<TransferBase.Columns>();
                builder.AppendEquals(TransferBase.Columns.TransId, transId.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiInTransferController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transfer ID '{0}' could not be found.", transId));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.LotList;
                await this.FilterEntityListAsync(list, ApiInTransferLotController.FunctionId);
            }

            if (id.HasValue)
                return list.FindAll(TransferLotBase.Columns.SeqNum, id.Value);

            return list;
        }

        protected virtual async Task<TransferLot> Find(int transId, int id)
        {
            var list = await Load(transId, id);
            return list.Find(x => x.SeqNum == id);
        }

        protected virtual async Task<List<TransferLot>> ProcessEditRequest(bool isCreate, dynamic body, int transId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Seq Num is provided along with more than one record.");

            var entityList = new List<TransferLot>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            RecalculateTotals();
            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransferLot> ProcessBodyItem(bool isCreate, dynamic bodyItem, int transId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum) || bodyItem.SeqNum == null)
                bodyItem.SeqNum = code;
            else
                code = Convert.ToInt32(bodyItem.SeqNum);

            var entity = await this.Find(transId, code);

            if (CurrentTransaction != null)
            {
                if (CurrentTransaction.ItemFromInfo == null
                    || !CurrentTransaction.ItemFromInfo.IsLotted
                    || CurrentTransaction.ItemFromInfo.InventoryType != InventoryType.Regular)
                    throw new InvalidValueException("This item is not a regular lotted item and cannot be processed.");
            }

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentTransaction?.LotList?.AddNew();
                entity.QtyFilled = CurrentTransaction?.Qty.GetValueOrDefault() - CurrentTransaction?.LotList?.Sum(l => l.QtyFilled.GetValueOrDefault());
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Seq Num {0} could not be found on Transfer ID '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int transId, int id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Seq Num {0} could not be found on Transfer ID '{1}'.", id, transId));

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

        protected virtual void LotNumberFromPropertyChanged(TransferLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNumFrom)
                || CurrentTransaction.ItemFromInfo == null
                || !CurrentTransaction.ItemFromInfo.IsLotted)
                return;

            Lot lot = CurrentTransaction.ItemFromInfo?.GetLocationById(CurrentTransaction.LocIdFrom)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNumFrom, true);

            if (lot == null)
                throw new InvalidValueException(string.Format("Lot Number '{0}' is not on file.", entity.LotNumFrom));

            ValidateQty(entity);
        }

        protected virtual void LotNumberToPropertyChanged(TransferLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNumTo)
                || CurrentTransaction.ItemFromInfo == null
                || !CurrentTransaction.ItemFromInfo.IsLotted)
                return;

            Lot lot = CurrentTransaction.ItemFromInfo?.GetLocationById(CurrentTransaction.LocIdTo)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNumTo, true);

            if (entity.IsNew && lot == null)
            {
                lot = this.CreateBrandNewLot(entity.ItemId, CurrentTransaction.LocIdTo, entity.LotNumTo);
                LotProvider provider = new LotProvider();
                provider.Items.Add(lot);
                provider.Update(this.CompId);
            }
        }

        protected virtual void ValidateQty(TransferLot entity)
        { }

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
            Action<TransferLot> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransferLot);
        }
        #endregion Event Handlers

        #region Properties
        protected TransferProvider Provider { get; } = new TransferProvider();

        protected Transfer CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransferLot>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransferLot>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionId = "F4416E23-775B-4928-9518-0F1000FDA003";
        #endregion Fields
    }
}
