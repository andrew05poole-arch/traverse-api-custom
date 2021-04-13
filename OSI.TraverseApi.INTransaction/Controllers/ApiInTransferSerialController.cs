#region Using Directives
using OSI.TraverseApi.Business;
using System;
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
    public class ApiInTransferSerialController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transfer/{transid:int}/serial/{sernum?}", typeof(TransferSerial))]
        public async Task<IHttpActionResult> Get(int transId, string sernum = null)
        {
            return Ok(await Load(transId, sernum));
        }

        [ApiRoute(FunctionID, 2f, "transfer/{transid:int}/serial/{sernum?}", typeof(TransferSerial))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int transId, string sernum = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, sernum));
        }

        [ApiRoute(FunctionID, 2f, "transfer/{transid:int}/serial/{sernum?}", typeof(TransferSerial))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int transId, string sernum = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, sernum));
        }

        [ApiRoute(FunctionID, 2f, "transfer/{transid:int}/serial/{sernum}", typeof(TransferSerial))]
        public async Task Delete(int transId, string sernum)
        {
            await MarkToDelete(transId, sernum);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransferSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            PropertyDictionary.Add(TransferSerialBase.Columns.LotNumTo.ToString(), SerialLotNumberToPropertyChanged);

            EntityPropertyDictionary.Add(TransferSerialBase.Columns.LotNumTo.ToString(), EntitySerialPropertyChanged);
        }

        protected virtual async Task<EntityList<TransferSerial>> Load(int transId, string id)
        {
            var list = CurrentTransaction?.SerialList as EntityList<TransferSerial>;

            if (CurrentTransaction == null || CurrentTransaction.TransId != transId)
            {
                var builder = new SqlFilterBuilder<TransferBase.Columns>();
                builder.AppendEquals(TransferBase.Columns.TransId, transId.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiInTransferController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transfer ID '{0}' could not be found.", transId));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.SerialList as EntityList<TransferSerial>;
                await this.FilterEntityListAsync(list, ApiInTransferSerialController.FunctionID);
            }

            if (!string.IsNullOrEmpty(id))
                return list?.FindAll(TransactionSerialBase.Columns.SerNum, id, true);

            return list;
        }

        protected virtual async Task<TransferSerial> Find(int transId, string id)
        {
            var list = await Load(transId, id);
            return list?.Find(x => StringHelper.AreEqual(x.SerNum, id, false));
        }

        protected virtual async Task<List<TransferSerial>> ProcessEditRequest(bool isCreate, dynamic body, int transId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. Seq Num is provided along with more than one record.");

            var entityList = new List<TransferSerial>();
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

        protected virtual async Task<TransferSerial> ProcessBodyItem(bool isCreate, dynamic bodyItem, int transId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum) || string.IsNullOrWhiteSpace(bodyItem.SerNum))
                bodyItem.SerNum = code;
            else
                code = bodyItem.SerNum;

            var entity = await this.Find(transId, code);

            if (CurrentTransaction != null)
            {
                if (CurrentTransaction.ItemFromInfo == null 
                    || CurrentTransaction.ItemFromInfo.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("This item is not a serialized item and cannot be processed.");
            }

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = (CurrentTransaction?.SerialList as EntityList<TransferSerial>).AddNew();
                entity.ItemId = CurrentTransaction.ItemId;
                entity.LocId = CurrentTransaction.LocIdFrom;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Serial Num {0} could not be found on Transfer ID '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int transId, string id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Serial Num {0} could not be found on Transfer ID '{1}'.", id, transId));

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

        protected virtual void SerialNumberPropertyChanged(TransferSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || CurrentTransaction.ItemFromInfo == null
                || CurrentTransaction.ItemFromInfo.InventoryType != InventoryType.Serial)
                return;

            var serial = CurrentTransaction.ItemFromInfo?.GetLocationById(CurrentTransaction.LocIdFrom)?
                .SerialItems?.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            if (serial == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' is not on file.", entity.SerNum));

            if (string.IsNullOrEmpty(entity.LotNumFrom))
                entity.LotNumFrom = serial.LotNum;

            entity.SetCostDefault();
        }

        protected virtual void SerialLotNumberToPropertyChanged(TransferSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNumTo)
                || CurrentTransaction.ItemFromInfo == null
                || !CurrentTransaction.ItemFromInfo.IsLotted)
                return;

            Lot lot = CurrentTransaction.ItemFromInfo?.GetLocationById(CurrentTransaction.LocIdTo)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNumTo, true);

            if (entity.IsNew && lot == null && Utility.GetLotNumberBehavior(this.CompId) == 0)
            {
                lot = this.CreateBrandNewLot(CurrentTransaction.ItemIdFrom, CurrentTransaction.LocIdTo, entity.LotNumTo);
                LotProvider provider = new LotProvider();
                provider.Items.Add(lot);
                provider.Update(this.CompId);
            }
        }

        protected virtual void EntitySerialPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransferSerial serialInfo)
                {
                    if (serialInfo.IsNew && !((Transfer)serialInfo.Parent).ItemFromInfo.IsLotted)
                        e.Handled = true;
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
            Action<TransferSerial> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransferSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected TransferProvider Provider { get; } = new TransferProvider();

        protected Transfer CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransferSerial>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransferSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "742F69C8-4A18-469F-BE04-C912514037AD";
        #endregion Fields
    }
}
