#region Using Directives
using TRAVERSE.Web.API.PurchaseOrder.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoTransactionReceiptLotController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{id:int?}", typeof(TransactionLotReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Get(string transId, string receiptNum, int? id = null)
        {
            return Ok(await Load(transId, receiptNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{id:int?}", typeof(TransactionLotReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId, string receiptNum, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, receiptNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{id:int?}", typeof(TransactionLotReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, string receiptNum, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, receiptNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{id:int}", typeof(TransactionLotReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task Delete(string transId, string receiptNum, int id)
        {
            await this.MarkToDelete(transId, receiptNum, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Receipt Lot Property Changed
            PropertyDictionary.Add(TransactionLotReceiptBase.Columns.EntryNum.ToString(), EntryNumPropertyChanged);
            PropertyDictionary.Add(TransactionLotReceiptBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            PropertyDictionary.Add(TransactionLotReceiptBase.Columns.QtyFilled.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            PropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            PropertyDictionary.Add(TransactionLotReceiptBase.Columns.ExtCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateUnitCost();
                entity.SuppressEntityEvents = false;
            });

            //Recepit Serial Property Changed
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            SerialPropertyDictionary.Add(TransactionSerialBase.Columns.RcptUnitCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateReceiptBaseCost();
                entity.SuppressEntityEvents = false;
            });

            EntityPropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), ValidateHandledProperty);
            EntityPropertyDictionary.Add(TransactionLotReceiptBase.Columns.ExtCostFgn.ToString(), ValidateHandledProperty);
        }

        protected virtual async Task Load(string transId, string receiptNum)
        {
            if (this.CurrentReceipt == null
                || !StringHelper.AreEqual(this.CurrentReceipt.TransId, transId, false) || !StringHelper.AreEqual(this.CurrentReceipt.ReceiptNum, receiptNum, false))
            {
                EntityList<TransactionReceipt> receiptList = new EntityList<TransactionReceipt>();

                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);

                var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (list?.Count > 0)
                {
                    await this.FilterEntityListAsync(list, ApiPoTransactionHeaderController.FunctionID);
                    receiptList = list[0]?.GetReceiptList();

                    if (receiptList.Count > 0)
                    {
                        await this.FilterEntityListAsync(receiptList, ApiPoTransactionReceiptController.FunctionID);

                        this.CurrentReceipt = receiptList.Find(x => StringHelper.AreEqual(x.ReceiptNum, receiptNum, false));

                        if (this.CurrentReceipt != null)
                            this.Provider.Items.Add(this.CurrentReceipt);
                    }

                    if (receiptList.Count <= 0 || this.CurrentReceipt == null)
                        throw new InvalidValueException("Receipt Number could not be found.");
                }
                else
                    throw new InvalidValueException("Transaction ID could not be found.");
            }
        }

        protected virtual async Task<EntityList<TransactionLotReceipt>> Load(string transId, string receiptNum, int? id)
        {
            var list = this.CurrentReceipt?.LotReceiptList;

            if (this.CurrentReceipt == null || !StringHelper.AreEqual(this.CurrentReceipt.TransId, transId, false) 
                || !StringHelper.AreEqual(this.CurrentReceipt.ReceiptNum, receiptNum, false))
            {
                await Load(transId, receiptNum);

                list = this.CurrentReceipt?.LotReceiptList;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                list = list?.FindAll(TransactionLotReceiptBase.Columns.EntryNum, id.Value, true);

            return list;
        }

        protected virtual async Task<TransactionLotReceipt> Find(string transId, string receiptNum, int? id)
        {
            var list = await Load(transId, receiptNum, id);
            return list?.Find(x => x.EntryNum == id.Value);
        }

        protected virtual async Task<List<TransactionLotReceipt>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string receiptNum, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Entry Number is provided along with more than one record.");

            var entityList = new List<TransactionLotReceipt>();

            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, receiptNum, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionLotReceipt> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string receiptNum, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum) || bodyItem.EntryNum == null)
                bodyItem.EntryNum = code;
            else
                code = Convert.ToInt32(bodyItem.EntryNum);

            var entity = await this.Find(transId, receiptNum, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentReceipt?.LotReceiptList.AddNew();
                entity.ReceiptId = Guid.NewGuid();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Entry Number '{0}' could not be found on receipt number '{1}' and transaction '{2}'.", code, receiptNum, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, string receiptNum, int? id)
        {
            var entity = await this.Find(transId, receiptNum, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Entry Number '{0}' could not be found on receipt number '{1}' and transaction '{2}'.", id, receiptNum, transId));

            this.CurrentReceipt.LotReceiptList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "SerialList")
            {
                var lotReceipt = (TransactionLotReceipt)args.ParentObject;

                if (lotReceipt.TransactionDetail?.INItemInfo == null || lotReceipt.TransactionDetail?.INItemInfo?.InventoryType != InventoryType.Serial)
                    throw new InvalidValueException("Current item does not support adding serial numbers.");

                if (lotReceipt.IsNew)
                    return this.CreateSerial(lotReceipt, args.ItemModel);
                else
                    return this.UpdateSerial(lotReceipt, args.ItemModel);
            }

            return null;
        }

        #region LotReceipt Update Methods
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

        protected virtual void EntryNumPropertyChanged(TransactionLotReceipt entity)
        {
            if (entity.TransactionDetail.POLineStatus == POLineStatus.Completed)
                throw new InvalidValueException(string.Format("Entry Number {0} has been completed and it cannot be received.", entity.EntryNum));

            entity.LotNum = null;
            entity.LotCmnt = null;
            entity.SetDefaults();

            Receipts.ReceiptHeaderList = this.Provider.Items;
            decimal? qtyFilled = entity.QtyFilled;
            decimal d = 0m;
            if ((qtyFilled.GetValueOrDefault() == d & qtyFilled != null) && entity.TransactionDetail != null)
            {
                decimal num = entity.TransactionDetail.QtyOrd.Value - Receipts.GetQty(entity.EntryNum);

                if (num >= 0m)
                    entity.QtyFilled = new decimal?(num);
            }
            entity.SetCostDefault();
        }

        protected virtual void LotNumberPropertyChanged(TransactionLotReceipt entity)
        {
            if (string.IsNullOrEmpty(entity.LotNum)
                || entity.TransactionDetail.INItemInfo == null
                || !entity.TransactionDetail.INItemInfo.IsLotted)
            {
                entity.LotNum = null;
                return;
            }

            Lot lot = entity.TransactionDetail.INItemInfo?.AllLocations?.Find(ItemLocationBase.Columns.LocId, entity.LocationId, true)?
                .LotNumbers?.Find(LotBase.Columns.LotNum, entity.LotNum, true);

            if (entity.IsNew && lot == null)
            {
                if (((TransactionHeader)entity.TransactionDetail.Parent)?.TransactionType > POTransactionType.Request)
                    entity.NewLot = this.CreateBrandNewLot(entity.ItemId, entity.LocationId, entity.LotNum);
                else
                    throw new InvalidValueException(string.Format("'{0}' is not on file.", entity.LotNum));
            }
            if (lot != null)
                this.ValidateLotExpired(lot, entity.TransactionReceipt.Date);

            entity.SetCostDefault();
        }

        protected virtual void ValidateLotExpired(Lot lot, DateTime date)
        {
            if (lot.ExpDate < date)
                AddWarnings(string.Format("Warning: Lot '{0}' is expired.", lot.LotNum));
        }
        #endregion LotReceipt Update Methods

        #region Serial Update Methods
        protected virtual TransactionSerial UpdateSerial(TransactionLotReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial Number is required.");

            this.FilterEntityList(parent.SerialList, ApiPoTransactionReceiptSerialController.FunctionID);

            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' for Entry Number '{1}' could not be found.", bodyItem.SerNum, parent.EntryNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionSerial CreateSerial(TransactionLotReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum))
                throw new InvalidValueException("Serial Number is required.");

            TransactionSerial entity = (parent.SerialList as EntityList<TransactionSerial>)?.Find(x => StringHelper.AreEqual(x.SerNum, bodyItem.SerNum, false));
            if (entity != null)
                throw new InvalidValueException(string.Format("Serial Number '{0}' for Entry Number '{1}' already exists.",
                    bodyItem.SerNum, parent.EntryNum));

            entity = (parent.SerialList as EntityList<TransactionSerial>).AddNew();
            entity.SetDefaultBin();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SerialUpdateComplete;
            entity.PropertyChanged += SerialEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void SerialNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || entity.TransactionLotReceipt?.TransactionDetail?.INItemInfo == null
                || entity.TransactionLotReceipt?.TransactionDetail?.INItemInfo.InventoryType != InventoryType.Serial)
                return;

            var serialItem = entity.TransactionLotReceipt?.TransactionDetail?.INItemInfo?.AllLocations?
                .Find(ItemLocationBase.Columns.LocId, entity.TransactionLotReceipt?.LocationId, true)?
                .SerialItems?.Find(SerialItemBase.Columns.SerNum, entity.SerNum, true);

            if (entity.IsNew && entity.TransactionLotReceipt != null)
            {
                if (!(entity.TransactionHeader?.TransactionType > POTransactionType.Request))
                {
                    if (serialItem != null && serialItem.SerialItemStatus == SerialItemStatus.Available)
                    {
                        serialItem.SerialItemStatus = SerialItemStatus.Returned;

                        if (entity.TransactionLotReceipt.TransactionDetail.INItemInfo.IsLotted)
                            entity.LotNum = serialItem.LotNum;
                    }
                    else
                        throw new InvalidValueException("Serial Number must have a status of Available to be returned.");
                }
                else
                {
                    if (serialItem != null)
                        throw new InvalidValueException("Serial Number already exists for this item.");
                }
            }
            entity.SetBinContainerDefault();
            entity.SetReceiptCostDefault();
            entity.CalculateReceiptBaseCost();
        }

        protected virtual void ValidateHandledProperty(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (e.Entity is TransactionLotReceipt lotReceipt)
                {
                    if (lotReceipt.TransactionDetail.INItemInfo.InventoryType == InventoryType.Serial)
                        e.Handled = true;
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
            var entity = sender as TransactionLotReceipt;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionLotReceipt> action = null;

            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);

            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void SerialEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionSerial> action = null;
            if (SerialPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionSerial);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionReceiptProvider Provider { get; } = new TransactionReceiptProvider();

        protected TransactionReceipt CurrentReceipt { get; set; }

        protected SortedDictionary<string, Action<TransactionLotReceipt>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLotReceipt>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "32a21dc9-ad28-4ce1-b83c-4c6b486a4395";
        #endregion Fields
    }
}
