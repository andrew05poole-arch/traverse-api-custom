#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using PC = TRAVERSE.Business.ProjectCosting;
using T = System.Threading.Tasks;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.ProjectCosting.Controllers
{
    public class ApiPcTransactionController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{id:int?}", typeof(Transaction), new object[] { ApiPcTransactionSerialController.FunctionID, typeof(TransactionSer) })]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id:int?}", typeof(Transaction), new object[] { ApiPcTransactionSerialController.FunctionID, typeof(TransactionSer) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id:int?}", typeof(Transaction), new object[] { ApiPcTransactionSerialController.FunctionID, typeof(TransactionSer) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id:int}", typeof(Transaction), new object[] { ApiPcTransactionSerialController.FunctionID, typeof(TransactionSer) })]
        public async T.Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Header Property Changes
            PropertyDictionary.Add(TransactionBase.Columns.TransType.ToString(), (entity) =>
            {
                entity.SetDefaults();
                entity.SetProjectDefaults(); 
            });
            PropertyDictionary.Add(TransactionBase.Columns.ProjectDetailId.ToString(), (entity) => entity.SetProjectDefaults());
            PropertyDictionary.Add(TransactionBase.Columns.ItemId.ToString(), ItemIdPropertyChanged);
            PropertyDictionary.Add(TransactionBase.Columns.QtyNeed.ToString(), QtyNeedPropertyChanged);
            PropertyDictionary.Add(TransactionBase.Columns.LocId.ToString(), (entity) =>
            {
                entity.SetItemLocDefaults();
                entity.SetCostDefault();
            });
            PropertyDictionary.Add(TransactionBase.Columns.QtyFilled.ToString(), (entity) =>
            {
                entity.SetCostDefault();
                entity.Calculate();
            });
            PropertyDictionary.Add(TransactionBase.Columns.TransDate.ToString(), TransDatePropertyChanged);
            PropertyDictionary.Add(TransactionBase.Columns.Uom.ToString(), (entity) =>
            {
                if (entity.InItem != null)
                    entity.SetCostDefault();
            });

            //Lot Item Property Changes
            LotPropertyDictionary.Add(TransactionExtBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionExtBase.Columns.ExtLocA.ToString(), (entity) =>
            {
                if (string.IsNullOrEmpty(entity.LotNum))
                    entity.SetCostDefault();
            });
            LotPropertyDictionary.Add(TransactionExtBase.Columns.ExtLocB.ToString(), (entity) =>
            {
                if (string.IsNullOrEmpty(entity.LotNum))
                    entity.SetCostDefault();
            });

            //Serial Item Property Changes
            SerialPropertyDictionary.Add(TransactionSerBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerBase.Columns.UnitCost.ToString(), (entity) => entity.CalculateCostACV());

            EntityPropertyDictionary.Add(TransactionSerBase.Columns.UnitCost.ToString(), ValidateCostUnitFgn);
            EntityPropertyDictionary.Add(TransactionSerBase.Columns.LotNum.ToString(), ValidateSerialLotNumber);
        }

        protected virtual async Task<EntityList<Transaction>> Load(int? id)
        {
            if (Provider.Items.Count <= 0 || !Provider.Items.Exists(i => i.Id == id))
            {
                if (!id.HasValue)
                    await Provider.Load<Transaction>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransactionBase.Columns>();
                    builder.AppendEquals(TransactionBase.Columns.Id, id.ToString());
                    var list = new TransactionProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Transaction> Find(int id)
        {
            var list = await Load(id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<Transaction>> ProcessEditRequest(bool isCreate, dynamic body, int? id)
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
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Transaction> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = new Transaction(this.CompId)
                {
                    QtyNeed = new decimal(1)
                };
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            entity.Calculate();

            return entity;
        }

        protected virtual async T.Task MarkToDelete(int id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transaction ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            var transaction = (Transaction)args.ParentObject;
            if (args.PropertyName == "ExtendedEntityList")
            {
                if (transaction.PCTransactionType == PC.TransactionType.Expense || transaction.PCTransactionType == PC.TransactionType.Other)
                    throw new InvalidValueException(string.Format("Current Transaction Type: '{0}' does not support adding extended items.", transaction.PCTransactionType));

                if (transaction.InItem == null)
                {
                    args.Ignore = true;
                    return null;
                }

                if (transaction.IsNew)
                    return this.CreateLot(transaction, args.ItemModel);
                else
                    return this.UpdateLot(transaction, args.ItemModel);
            }
            else if (args.PropertyName == "SerialList")
            {
                if (transaction.PCTransactionType == PC.TransactionType.Expense || transaction.PCTransactionType == PC.TransactionType.Other)
                    throw new InvalidValueException(string.Format("Current Transaction Type: '{0}' does not support adding serial numbers.", transaction.PCTransactionType));

                if (transaction.InItem == null || transaction.InItem.InventoryType != InventoryType.Serial)
                {
                    args.Ignore = true;
                    return null;
                }

                if (transaction.IsNew)
                    return this.CreateSerial(transaction, args.ItemModel);
                else
                    return this.UpdateSerial(transaction, args.ItemModel);
            }
       
            return null;
        }

        #region Header Update Methods
        protected virtual void ItemIdPropertyChanged(Transaction entity)
        {
            if (Utility.INYN)
            {
                if (entity.InItem != null)
                {
                    entity.SetItemDefaults();

                    if (entity.InItem.InventoryStatus == InventoryStatus.Obsolete)
                        throw new InvalidValueException("That item has an invalid status for this transaction.");
                    else if (entity.InItem.AllLocations.Count < 1)
                    {
                        entity.ItemId = null;
                        throw new InvalidValueException("That item is not setup at any locations.");
                    }
                    else
                    {
                        entity.InItem.GetQuantityAvailable();
                        if (entity.InItem.IsKit)
                            throw new InvalidValueException("Kitted items are not valid for this function.");
                    }
                }
                else if (!string.IsNullOrEmpty(entity.ItemId)
                    && (entity.PCTransactionType == PC.TransactionType.MaterialRequisition
                    || entity.PCTransactionType == PC.TransactionType.MaterialReturn))
                {
                    throw new InvalidValueException(string.Format("Item ID '{0}' does not exist in Inventory.", entity.ItemId));
                }
            }
            else if (entity.SmItem != null)
                entity.SetItemDefaults();
        }

        protected virtual void QtyNeedPropertyChanged(Transaction entity)
        {
            if (entity.PCTransactionType == PC.TransactionType.Expense || entity.PCTransactionType == PC.TransactionType.Other)
            {
                if (entity.IsNew && entity.InItem != null)
                {
                    if (entity.InItem.InventoryType != InventoryType.Serial || !entity.InItem.IsLotted)
                        entity.QtyFilled = entity.QtyNeed;
                }
                else if (entity.IsNew && entity.InItem == null)
                    entity.QtyFilled = entity.QtyNeed;
            }
            entity.SetCostDefault();
            entity.Calculate();
        }

        protected virtual void TransDatePropertyChanged(Transaction entity)
        {
            short period = (short)entity.TransDate.Month;
            short year = (short)entity.TransDate.Year;

            PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(period, year);
            if (fiscalPeriod != null)
            {
                if (fiscalPeriod.ClosedGL.Value)
                    AddWarnings(string.Format("Period {0} is closed for fiscal year {1}.", period, year));
            }
        }
        #endregion Transaction Methods

        #region Lot Update Methods
        protected virtual TransactionExt UpdateLot(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("ID is required.");

            this.FilterEntityList(parent.ExtendedEntityList, ApiPcTransactionLotController.FunctionID);
            TransactionExt entity = parent.ExtendedEntityList.Find(x => x.Id == (int)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Lot '{0}' for Transaction ID '{1}' could not be found.", bodyItem.Id, parent.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionExt CreateLot(Transaction parent, dynamic bodyItem)
        {
            TransactionExt entity = parent.ExtendedEntityList.AddNew();
            entity.QtyNeed = parent.QtyNeed;

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

        protected virtual void LotNumberPropertyChanged(TransactionExt entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || entity.InItem == null
                || !entity.InItem.IsLotted)
                return;

            Lot lot = entity.InItem?.AllLocations?.Find(ItemLocationBase.Columns.LocId, entity.LocId, true)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && lot == null)
            {
                if (((Transaction)entity.ParentEntity).PCTransactionType == PC.TransactionType.MaterialReturn)
                    entity.NewLot = this.CreateBrandNewLot(entity.InItem.ItemId, entity.LocId, entity.LotNum);
                else
                    throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.LotNum));
            }
            entity.SetCostDefault();
        }

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionExt;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
        }
        #endregion Lot Update Methods

        #region Serial Update Methods
        protected virtual TransactionSer UpdateSerial(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            this.FilterEntityList(parent.SerialList, ApiPcTransactionSerialController.FunctionID);
            TransactionSer entity = (parent.SerialList as EntityList<TransactionSer>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Transaction ID '{1}' could not be found.",
                    bodyItem.SerNum, parent.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionSer CreateSerial(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial number is required.");

            TransactionSer entity = (parent.SerialList as EntityList<TransactionSer>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Serial number '{0}' for Transaction ID '{1}' already exists.",
                    bodyItem.SerNum, parent.Id));

            entity = (parent.SerialList as EntityList<TransactionSer>).AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
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
                else if (e.Entity is TransactionExt lotInfo)
                {
                    if (!((Transaction)lotInfo.ParentEntity).InItem.IsLotted)
                        e.Handled = true;
                }
            }
        }

        protected virtual void SerialUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionSer;
            entity.PropertyChanged -= SerialEntity_PropertyChanged;
        }
        #endregion Serial Update Methods

        protected virtual void ValidateCostUnitFgn(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is Transaction transInfo)
                {
                    if (transInfo.PCTransactionType == PC.TransactionType.MaterialRequisition)
                        e.Handled = true;
                }
                else if (e.Entity is TransactionSer serialInfo)
                {
                    if (((Transaction)serialInfo.ParentEntity).PCTransactionType == PC.TransactionType.MaterialRequisition)
                        e.Handled = true;
                }
                else
                {
                    if (e.Entity is TransactionExt lotInfo)
                        if (((Transaction)lotInfo.ParentEntity).PCTransactionType == PC.TransactionType.MaterialRequisition)
                            e.Handled = true;
                }
            }
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
            var entity = sender as Transaction;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<Transaction> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionExt> action = null;
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionExt);
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSer> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSer);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionProvider Provider { get; } = new TransactionProvider();

        protected SortedDictionary<string, Action<Transaction>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Transaction>>();

        protected SortedDictionary<string, Action<TransactionExt>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionExt>>();

        protected SortedDictionary<string, Action<TransactionSer>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSer>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "686D3A6E-C2D0-47E0-B143-57DD66890406";
        #endregion Fields
    }
}
