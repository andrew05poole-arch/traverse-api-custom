#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using PC = TRAVERSE.Business.ProjectCosting;
using T = System.Threading.Tasks;
#endregion Using Directives

namespace TRAVERSE.Web.API.ProjectCosting.Controllers
{
    public class ApiPcTransactionSerialController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/serial/{sernum?}", typeof(TransactionSer))]
        public async Task<IHttpActionResult> Get(int transId, string sernum = null)
        {
            return Ok(await Load(transId, sernum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/serial/{sernum?}", typeof(TransactionSer))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int transId, string sernum = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, sernum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/serial/{sernum?}", typeof(TransactionSer))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int transId, string sernum = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, sernum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/serial/{sernum}", typeof(TransactionSer))]
        public async T.Task Delete(int transId, string sernum)
        {
            await MarkToDelete(transId, sernum);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Serial Item Property Changes
            PropertyDictionary.Add(TransactionSerBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            PropertyDictionary.Add(TransactionSerBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            PropertyDictionary.Add(TransactionSerBase.Columns.UnitCost.ToString(), (entity) => entity.CalculateCostACV());

            EntityPropertyDictionary.Add(TransactionSerBase.Columns.UnitCost.ToString(), ValidateCostUnitFgn);
            EntityPropertyDictionary.Add(TransactionSerBase.Columns.LotNum.ToString(), ValidateSerialLotNumber);
        }

        protected virtual async Task<EntityList<TransactionSer>> Load(int transId, string id)
        {
            var list = CurrentTransaction?.SerialList as EntityList<TransactionSer>;

            if (CurrentTransaction == null || CurrentTransaction.Id != transId)
            {
                var builder = new SqlFilterBuilder<TransactionBase.Columns>();
                builder.AppendEquals(TransactionBase.Columns.Id, transId.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPcTransactionController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", transId));

                CurrentTransaction = Provider.Items[0];

                list = CurrentTransaction.SerialList as EntityList<TransactionSer>;
                await this.FilterEntityListAsync(list, ApiPcTransactionSerialController.FunctionID);
            }

            if (!string.IsNullOrEmpty(id))
                return list?.FindAll(TransactionSerBase.Columns.SerNum, id, true);

            return list;
        }

        protected virtual async Task<TransactionSer> Find(int transId, string id)
        {
            var list = await Load(transId, id);
            return list?.Find(x => StringHelper.AreEqual(x.SerNum, id, false));
        }

        protected virtual async Task<List<TransactionSer>> ProcessEditRequest(bool isCreate, dynamic body, int transId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. Serial No is provided along with more than one record.");

            var entityList = new List<TransactionSer>();
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

        protected virtual async Task<TransactionSer> ProcessBodyItem(bool isCreate, dynamic bodyItem, int transId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum) || string.IsNullOrWhiteSpace(bodyItem.SerNum))
                bodyItem.SerNum = code;
            else
                code = bodyItem.SerNum;

            var entity = await this.Find(transId, code);

            if (CurrentTransaction != null)
            {
                if (CurrentTransaction.PCTransactionType == PC.TransactionType.Expense || CurrentTransaction.PCTransactionType == PC.TransactionType.Other)
                    throw new InvalidValueException(string.Format("Current Transaction Type: '{0}' does not support adding serial numbers.", CurrentTransaction.PCTransactionType));

                if (CurrentTransaction.InItem == null
                    || CurrentTransaction.InItem.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("This item is not a serialized item and cannot be processed.");
            }

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = (CurrentTransaction?.SerialList as EntityList<TransactionSer>).AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Serial No {0} could not be found on Transaction ID '{1}'.", code, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(int transId, string id)
        {
            var entity = await this.Find(transId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Serial No {0} could not be found on Transaction ID '{1}'.", id, transId));

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

        protected virtual void SerialNumberPropertyChanged(TransactionSer entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || ((Transaction)entity.ParentEntity).InItem == null
                || ((Transaction)entity.ParentEntity).InItem.InventoryType != InventoryType.Serial)
                return;

            var serial = ((Transaction)entity.ParentEntity).InItem?.AllLocations?.Find(ItemLocationBase.Columns.LocId, ((Transaction)entity.ParentEntity).LocId, true)?
                .SerialItems?.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            if (string.IsNullOrEmpty(entity.LotNum))
                entity.LotNum = serial?.LotNum;

            if (entity.Transaction.PCTransactionType == PC.TransactionType.MaterialRequisition)
            {
                if (serial == null)
                    throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.SerNum));

                else if (serial != null && serial.SerialItemStatus != SerialItemStatus.Available)
                    throw new InvalidValueException(string.Format("Serial Number must have a status of Available to be sold."));

                else if (serial != null && serial.SerialItemStatus == SerialItemStatus.Available)
                {
                    serial.SerialItemStatus = SerialItemStatus.Sold;
                    entity.SetDefaults();
                }
            }
            else if (((Transaction)entity.ParentEntity).PCTransactionType == PC.TransactionType.MaterialReturn)
            {
                if (serial != null)
                {
                    if (serial.SerialItemStatus == SerialItemStatus.Sold)
                    {
                        serial.SerialItemStatus = SerialItemStatus.Returned;
                        entity.SetDefaults();
                    }
                    else
                        throw new InvalidValueException(string.Format("Serial Number must have a status of Sold to be returned."));
                }
            }
            entity.SetCostDefault();
        }

        protected virtual void LotNumberPropertyChanged(TransactionSer entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || ((Transaction)entity.ParentEntity).InItem == null
                || !((Transaction)entity.ParentEntity).InItem.IsLotted)
                return;

            Lot lot = ((Transaction)entity.ParentEntity).InItem?.AllLocations?.Find(ItemLocationBase.Columns.LocId, ((Transaction)entity.ParentEntity).LocId, true)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && lot == null)
            {
                if (((Transaction)entity.ParentEntity).PCTransactionType == PC.TransactionType.MaterialReturn)
                    entity.NewLot = this.CreateBrandNewLot(((Transaction)entity.ParentEntity).InItem.ItemId, ((Transaction)entity.ParentEntity).LocId, entity.LotNum);
            }
        }

        protected virtual void ValidateSerialLotNumber(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionSer serialInfo)
                {
                    if (((Transaction)serialInfo.ParentEntity).PCTransactionType == PC.TransactionType.MaterialRequisition)
                    {
                        e.Handled = true;
                    }
                    else if (((Transaction)serialInfo.ParentEntity).PCTransactionType == PC.TransactionType.MaterialReturn
                              && !string.IsNullOrEmpty(serialInfo.SerNum))
                    {
                        Transaction transaction = (Transaction)serialInfo.ParentEntity;

                        if (transaction != null)
                        {
                            var serial = transaction.InItem?.AllLocations?.Find(ItemLocationBase.Columns.LocId, transaction.LocId, true)?
                                    .SerialItems?.Find(SerialItemBase.Columns.SerNum, serialInfo.SerNum, true);

                            if (serial != null)
                                e.Handled = true;
                        }
                    }
                    if (!((Transaction)serialInfo.ParentEntity).InItem.IsLotted)
                        e.Handled = true;
                }
            }
        }

        protected virtual void ValidateCostUnitFgn(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionSer serialInfo)
                {
                    if (((Transaction)serialInfo.ParentEntity).PCTransactionType == PC.TransactionType.MaterialRequisition)
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
            Action<TransactionSer> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSer);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionProvider Provider { get; } = new TransactionProvider();

        protected Transaction CurrentTransaction { get; set; }

        protected SortedDictionary<string, Action<TransactionSer>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSer>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "6D382353-E38E-47AB-A10D-6BCFECC49BBC";
        #endregion Fields
    }
}
