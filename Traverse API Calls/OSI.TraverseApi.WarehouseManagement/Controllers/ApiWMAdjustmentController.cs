#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMAdjustmentController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "adjustment/{id:int?}", typeof(Transaction))]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "adjustment/{id:int?}", typeof(Transaction))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "adjustment/{id:int?}", typeof(Transaction))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "adjustment/{id:int}", typeof(Transaction))]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overriders
        protected override void AddPropertyDelegates() 
        {
            this.EntityPropertyDictionary.Add(TransactionBase.Columns.ExtLocA.ToString(), ExtLocAPropertyChanged);
            this.EntityPropertyDictionary.Add(TransactionBase.Columns.ExtLocB.ToString(), ExtLocBPropertyChanged);
            this.SerialPropertyDictionary.Add(TransactionSerialBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Transaction)
            {
                if (StringHelper.AreEqual(args.FieldName, "ExtLocA", false) || StringHelper.AreEqual(args.FieldName, "ExtLocB", false))
                {
                    if (args.ActualValue != null)
                    {
                        var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                        builder.AppendEquals(ExtLocationBase.Columns.Id, args.ActualValue.ToString());
                        builder.AppendEquals(ExtLocationBase.Columns.LocId, (args.Entity as Transaction).LocId);
                        args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].ExtLocId;
                    }
                }
            }
        }
        #endregion Overriders

        protected virtual async Task<EntityList<Transaction>> Load(int? id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.TransId == id)))
            {
                if (id == null)
                    await Provider.Load<Transaction>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransactionBase.Columns>();
                    builder.AppendEquals(TransactionBase.Columns.TransId, id.ToString());
                    var list = new TransactionProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<Transaction> Find(int id)
        {
            var list = await Load(id);
            return list.Find(x => x.TransId == id);
        }

        protected virtual async Task<List<Transaction>> ProcessEditRequest(bool isCreate, dynamic body, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Transaction ID is provided along with more than one record.");

            var entityList = new List<Transaction>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);

                await ValidateEntityListAsync(entityList);
                Provider.Update(this.CompId);
            }

            return entityList;
        }

        protected virtual async Task<Transaction> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || bodyItem.TransId == null)
                bodyItem.TransId = code;
            else
                code = Convert.ToInt32(bodyItem.TransId);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Transaction(this.CompId);
                entity.ItemId = !ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId) ? bodyItem.ItemId : null;
                if (!ApiUserSkipped.IsApiUserSkipped(bodyItem.TransType))
                {
                    if (bodyItem.TransType == "Increase")
                        entity.TransType = (byte)WMTransactionType.Increase;
                    else
                        entity.TransType = (byte)WMTransactionType.Decrease;
                }
                else
                    entity.TransType = (byte)WMTransactionType.Increase;

                entity.BatchId = !ApiUserSkipped.IsApiUserSkipped(bodyItem.BatchId) ? bodyItem.BatchId : Utility.GlobalBatchId;
                entity.SetItemDefault();               
                entity.SetDefaults();
                entity.SetExhangeRateTransDefaultList();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Transaction ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "SerialList")
            {
                if (((Transaction)args.ParentObject).IsNew)
                    return this.CreateSerial((Transaction)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateSerial((Transaction)args.ParentObject, args.ItemModel);
            }
            return null;
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
            entity.SetPriceCostDefault();
            entity.SetBinContainerDefault();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionSerial UpdateSerial(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            this.FilterEntityList(parent.SerialList, ApiWMAdjustmentSerialItemController.FunctionId);
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

        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionSerial;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
        }

        protected virtual void LotNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || ((Transaction)entity.Parent).ItemInfo == null
                || !((Transaction)entity.Parent).ItemInfo.IsLotted)
                return;

            Lot lot = ((Transaction)entity.Parent).ItemInfo.AllLocations.Find(ItemLocationBase.Columns.LocId, entity.LocId)?.LotNumbers.Find(LotBase.Columns.LotNum, entity.LotNum, false);

            if (entity.IsNew &&
                lot == null)
            {
                entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocId, entity.LotNum);
            }
            else
            {
                if (lot == null)
                    throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.LotNum));

                if (lot.ExpDate.GetValueOrDefault(DateTime.Today) < DateTime.Today)
                    this.AddWarnings("Lot is expired.");
            }
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

        #region Body Item Update Methods
        protected virtual void ExtLocAPropertyChanged(dynamic body, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Transaction)
            {
                if (body.ExtLocA != null)
                {
                    var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();

                    if (StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocA.ToString(), false))
                        builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, body.ExtLocA);

                    builder.AppendEquals(ExtLocationBase.Columns.LocId, (args.Entity as Transaction).LocId);
                    args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].Id;
                }
            }
        }

        protected virtual void ExtLocBPropertyChanged(dynamic body, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Transaction)
            {
                if (body.ExtLocB != null)
                {
                    var builder = new SqlFilterBuilder<ExtLocationBase.Columns>(); ;
                    if (StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocB.ToString(), false))
                        builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, body.ExtLocB);

                    builder.AppendEquals(ExtLocationBase.Columns.LocId, (args.Entity as Transaction).LocId);
                    args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].Id;
                }
            }
        }
        #endregion Body Item Update Methods
        #endregion Helper Methods

        #region Event Handler
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

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSerial> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerial);
        }
        #endregion Event Handler

        #region Properties
        private TransactionProvider Provider { get; } = new TransactionProvider();
        protected SortedDictionary<string, Action<Transaction>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Transaction>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "72a160c7-962d-43e0-80e5-f8a55675be91";
        #endregion Fields
    }
}
