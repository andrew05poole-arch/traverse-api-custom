#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.INTransaction;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.INTransaction
{
    public class ApiInTransferController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transfer/{id:int?}", typeof(Transfer), new object[] { ApiInTransferSerialController.FunctionID, typeof(TransferSerial) })]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transfer/{id:int?}", typeof(Transfer), new object[] { ApiInTransferSerialController.FunctionID, typeof(TransferSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transfer/{id:int?}", typeof(Transfer), new object[] { ApiInTransferSerialController.FunctionID, typeof(TransferSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transfer/{id:int}", typeof(Transfer), new object[] { ApiInTransferSerialController.FunctionID, typeof(TransferSerial) })]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TransferBase.Columns.ItemIdFrom.ToString(), ItemIdFromPropertyChanged);
            PropertyDictionary.Add(TransferBase.Columns.XferDate.ToString(), XferDatePropertyChanged);

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransferLotBase.Columns.LotNumFrom.ToString(), LotNumberFromPropertyChanged);
            LotPropertyDictionary.Add(TransferLotBase.Columns.LotNumTo.ToString(), LotNumberToPropertyChanged);

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransferSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransferSerialBase.Columns.LotNumTo.ToString(), SerialLotNumberToPropertyChanged);

            EntityPropertyDictionary.Add(TransferSerialBase.Columns.LotNumTo.ToString(), EntitySerialPropertyChanged);
        }

        protected virtual async Task<EntityList<Transfer>> Load(int? id)
        {
            if (Provider.Items.Count <= 0 || (id.HasValue && !Provider.Items.Exists(i => i.TransId == id)))
            {
                if (!id.HasValue)
                    await Provider.Load<Transfer>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransferBase.Columns>();
                    builder.AppendEquals(TransferBase.Columns.TransId, id?.ToString());
                    var list = await new TransferProvider().Load<Transfer>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }

        protected virtual async Task<Transfer> Find(int id)
        {
            var list = await Load(id);
            return list.Find(x => x.TransId == id);
        }

        protected virtual async Task<List<Transfer>> ProcessEditRequest(bool isCreate, dynamic body, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Transfer ID is provided along with more than one record.");

            var entityList = new List<Transfer>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Transfer> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || bodyItem.TransId == null)
                bodyItem.TransId = code;
            else
                code = Convert.ToInt32(bodyItem.TransId);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                string batchId = Utility.GetUserTransferBatchCode(this.CompId);
                if (string.IsNullOrEmpty(batchId) || Utility.GetUseBatch(this.CompId))
                    batchId = TransferBatch.GetDefaultBatchId(Utility.FunctionIdTransfer, Utility.UseBatch, this.CompId, null);

                entity = new Transfer(this.CompId);
                entity.SetDefaults();
                entity.SetDefaults();
                entity.SetItemDefault();
                entity.SetQtyBefore();
                entity.Qty = 1;
                entity.BatchId = batchId;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transfer ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transfer ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            var transfer = (Transfer)args.ParentObject;
            if (args.PropertyName == "LotList")
            {
                if (transfer.ItemFromInfo == null || !transfer.ItemFromInfo.IsLotted || transfer.ItemFromInfo.InventoryType != InventoryType.Regular)
                    throw new InvalidValueException("This item is not a regular lotted item and cannot be processed.");

                if (transfer.IsNew)
                    return this.CreateLot(transfer, args.ItemModel);
                else
                    return this.UpdateLot(transfer, args.ItemModel);
            }
            else if (args.PropertyName == "SerialList")
            {
                if (transfer.ItemFromInfo == null || transfer.ItemFromInfo.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("This item is not a serialized item and cannot be processed.");

                if (transfer.IsNew)
                    return this.CreateSerial(transfer, args.ItemModel);
                else
                    return this.UpdateSerial(transfer, args.ItemModel);
            }
            return null;
        }

        protected virtual TransferLot UpdateLot(Transfer parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SeqNum))
                throw new InvalidValueException("Sequence number is required.");

            this.FilterEntityList(parent.LotList, ApiInTransferLotController.FunctionId);

            TransferLot entity = parent.LotList.Find(x => x.SeqNum == (int)bodyItem.SeqNum);
            if (entity == null)
                throw new InvalidValueException(string.Format("Lot '{0}' for Transfer ID '{1}' could not be found.", bodyItem.SeqNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransferLot CreateLot(Transfer parent, dynamic bodyItem)
        {
            TransferLot entity = parent.LotList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransferSerial UpdateSerial(Transfer parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            this.FilterEntityList(parent.SerialList, ApiInTransferSerialController.FunctionID);

            TransferSerial entity = (parent.SerialList as EntityList<TransferSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Serial No '{0}' for Transfer ID '{2}' could not be found.", bodyItem.SerNum, parent.TransId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransferSerial CreateSerial(Transfer parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            TransferSerial entity = (parent.SerialList as EntityList<TransferSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Transfer ID '{2}' already exists.",
                    bodyItem.SerNum, parent.TransId));

            entity = (parent.SerialList as EntityList<TransferSerial>).AddNew();
            entity.ItemId = parent.ItemId;
            entity.LocId = parent.LocIdFrom;

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

        protected virtual void ItemIdFromPropertyChanged(Transfer entity)
        {
            if (!string.IsNullOrEmpty(entity.ItemIdFrom))
                entity.SetItemDefault();
        }

        protected virtual void XferDatePropertyChanged(Transfer entity)
        {
            if (entity.XferDate != null && entity.XferDate is DateTime)
            {
                entity.GLPeriod = new short?(PeriodConversion.GetFiscalPeriod(entity.XferDate.Value).FiscalPeriod);
                entity.SumYear = new short?(PeriodConversion.GetFiscalPeriod(entity.XferDate.Value).FiscalYear);
                if (entity.IsNew)
                {
                    entity.SetExhangeRateTransDefaultList();
                }
            }
        }

        #region Lot
        protected virtual void LotNumberFromPropertyChanged(TransferLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNumFrom)
                || ((Transfer)entity.Parent).ItemFromInfo == null
                || !((Transfer)entity.Parent).ItemFromInfo.IsLotted)
                return;

            Lot lot = ((Transfer)entity.Parent).ItemFromInfo?.GetLocationById(((Transfer)entity.Parent).LocIdFrom)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNumFrom, true);

            if (lot == null)
                throw new InvalidValueException(string.Format("Lot Number '{0}' is not on file.", entity.LotNumFrom));

            ValidateQty(entity);
        }

        protected virtual void LotNumberToPropertyChanged(TransferLot entity)
        {
            if (string.IsNullOrEmpty(entity.LotNumTo)
                || ((Transfer)entity.Parent).ItemFromInfo == null
                || !((Transfer)entity.Parent).ItemFromInfo.IsLotted)
                return;

            Lot lot = ((Transfer)entity.Parent).ItemFromInfo?.GetLocationById(((Transfer)entity.Parent).LocIdTo)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNumTo, true);

            if (entity.IsNew && lot == null)
            {
                lot = this.CreateBrandNewLot(entity.ItemId, ((Transfer)entity.Parent).LocIdTo, entity.LotNumTo);
                LotProvider provider = new LotProvider();
                provider.Items.Add(lot);
                provider.Update(this.CompId);
            }
        }

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransferLot;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
        }
        #endregion Lot

        #region Serial
        protected virtual void SerialNumberPropertyChanged(TransferSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || ((Transfer)entity.Parent).ItemFromInfo == null
                || ((Transfer)entity.Parent).ItemFromInfo.InventoryType != InventoryType.Serial)
                return;

            var serial = ((Transfer)entity.Parent).ItemFromInfo?.GetLocationById(((Transfer)entity.Parent).LocIdFrom)?
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
                || ((Transfer)entity.Parent).ItemFromInfo == null
                || !((Transfer)entity.Parent).ItemFromInfo.IsLotted)
                return;

            Lot lot = ((Transfer)entity.Parent).ItemFromInfo?.GetLocationById(((Transfer)entity.Parent).LocIdTo)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNumTo, true);

            if (entity.IsNew && lot == null && Utility.GetLotNumberBehavior(this.CompId) == 0)
            {
                lot = this.CreateBrandNewLot(((Transfer)entity.Parent).ItemIdFrom, ((Transfer)entity.Parent).LocIdTo, entity.LotNumTo);
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

        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransferSerial;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
        }
        #endregion Serial

        protected virtual void ValidateQty(TransferLot entity)
        { }
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
            Action<Transfer> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Transfer);
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransferLot> action = null;
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransferLot);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransferSerial> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransferSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected TransferProvider Provider { get; } = new TransferProvider();

        protected SortedDictionary<string, Action<Transfer>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Transfer>>();

        protected SortedDictionary<string, Action<TransferLot>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransferLot>>();

        protected SortedDictionary<string, Action<TransferSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransferSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "F3D24FAC-B3E6-497B-984A-5B64922455F0";
        #endregion Fields
    }
}
