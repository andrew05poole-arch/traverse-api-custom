#region Using Directives
using OSI.TraverseApi.PurchaseOrder.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.PurchaseOrder.Controllers
{
    public class ApiPoTransactionReceiptController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum?}", typeof(TransactionReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Get(string transId, string receiptNum = null)
        {
            return Ok(await Load(transId, receiptNum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum?}", typeof(TransactionReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId, string receiptNum = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, receiptNum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum?}", typeof(TransactionReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, string receiptNum = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, receiptNum));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}", typeof(TransactionReceipt), new object[] { ApiPoTransactionReceiptSerialController.FunctionID, typeof(TransactionSerial) })]
        public async Task Delete(string transId, string receiptNum)
        {
            await this.MarkToDelete(transId, receiptNum);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Header Property Changed
            PropertyDictionary.Add(TransactionReceiptBase.Columns.ReceiptDate.ToString(), ReceiptDatePropertyChanged);
            PropertyDictionary.Add(TransactionReceiptBase.Columns.GlPeriod.ToString(), GLPeriodYearPropertyChanged);
            PropertyDictionary.Add(TransactionReceiptBase.Columns.FiscalYear.ToString(), GLPeriodYearPropertyChanged);

            //Receipt Lot Property Changed
            LotPropertyDictionary.Add(TransactionLotReceiptBase.Columns.EntryNum.ToString(), EntryNumPropertyChanged);
            LotPropertyDictionary.Add(TransactionLotReceiptBase.Columns.LotNum.ToString(), LotNumberPropertyChanged);
            LotPropertyDictionary.Add(TransactionLotReceiptBase.Columns.QtyFilled.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            LotPropertyDictionary.Add(TransactionLotReceiptBase.Columns.UnitCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateExtendedCost();
                entity.SuppressEntityEvents = false;
            });
            LotPropertyDictionary.Add(TransactionLotReceiptBase.Columns.ExtCostFgn.ToString(), (entity) =>
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

        protected virtual async Task<EntityList<TransactionReceipt>> Load(string transId, string receiptNum)
        {
            if (this.Provider.Items.Count <= 0
                || !this.Provider.Items.Exists(i => StringHelper.AreEqual(i.TransId, transId, false) && StringHelper.AreEqual(i.ReceiptNum, receiptNum, false)))
            {
                EntityList<TransactionReceipt> receiptList = new EntityList<TransactionReceipt>();

                var builder = new SqlFilterBuilder<TransactionHeaderBase.Columns>();
                builder.AppendEquals(TransactionHeaderBase.Columns.TransId, transId);

                var list = new TransactionHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (list?.Count > 0)
                {
                    receiptList = list[0]?.GetReceiptList();

                    if (receiptList.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(receiptNum))
                            receiptList = receiptList.FindAll(x => StringHelper.AreEqual(x.ReceiptNum, receiptNum, false));

                        this.Provider.Items.AddRange(receiptList);

                        await this.FilterEntityListAsync(this.Provider.Items);
                    }
                }
                else
                    throw new InvalidValueException("Transaction ID does not exist.");
            }
            return this.Provider.Items;
        }

        protected virtual async Task<TransactionReceipt> Find(string transId, string receiptNum)
        {
            var list = await Load(transId, receiptNum);
            return list.Find(x => StringHelper.AreEqual(x.TransId, transId, false) && StringHelper.AreEqual(x.ReceiptNum, receiptNum, false));
        }

        protected virtual async Task<List<TransactionReceipt>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string receiptNum)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(receiptNum))
                throw new InvalidValueException("Call is ambiguous. Receipt Number is provided along with more than one record.");

            var entityList = new List<TransactionReceipt>();
            this.TransId = transId;

            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, receiptNum);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);

            this.Provider?.Update(this.CompId);

            this.UpdateHeaderType();

            return entityList;
        }

        protected virtual async Task<TransactionReceipt> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string receiptNum)
        {
            string code = receiptNum;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ReceiptNum) || string.IsNullOrWhiteSpace(bodyItem.ReceiptNum))
                bodyItem.ReceiptNum = code;
            else
                code = bodyItem.ReceiptNum;

            var entity = await this.Find(transId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new TransactionReceipt(this.CompId);
                entity.TransactionHeader = this.POHeader;
                entity.TransId = this.TransId;
                entity.SetFiscalPeriodYearFromDate((DateTime)entity.ReceiptDate);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID '{0}' with Receipt Number '{1}' could not be found.", transId, receiptNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, string receiptNum)
        {
            var entity = await this.Find(transId, receiptNum);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Transaction ID '{0}' with Receipt Number '{1}' could not be found.", transId, receiptNum));

            entity.LotReceiptList.RemoveAll();
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "LotReceiptList")
            {
                var receipt = (TransactionReceipt)args.ParentObject;

                if (receipt.IsNew)
                    return this.CreateLotReceipt(receipt, args.ItemModel);
                else
                    return this.UpdateLotReceipt(receipt, args.ItemModel);
            }
            else if (args.PropertyName == "SerialList")
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

        #region Header Update Methods
        protected virtual void ReceiptDatePropertyChanged(TransactionReceipt entity)
        {
            entity.SetFiscalPeriodYearFromDate(entity.ReceiptDate.Value);
            this.ValidatePeriodConversion(entity);
            Receipts.SynchronizeProjectActivity(entity);
        }

        protected virtual void GLPeriodYearPropertyChanged(TransactionReceipt entity)
        {
            this.ValidatePeriodConversion(entity);
            Receipts.SynchronizeProjectActivity(entity);
        }

        protected virtual void ValidatePeriodConversion(TransactionReceipt entity)
        {
            short period = (short)((DateTime)entity.ReceiptDate).Month;
            short year = (short)((DateTime)entity.ReceiptDate).Year;

            PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(period, year);
            if (fiscalPeriod != null && fiscalPeriod.ClosedPO.Value)
            {
                if (fiscalPeriod.ClosedGL.Value)
                    AddWarnings(string.Format("Period {0} is closed for fiscal year {1}.", period, year));
            }
        }

        protected virtual void UpdateHeaderType()
        {
            if (this.POHeader.TransactionType == POTransactionType.NewOrder && this.POHeader.GetReceiptList().Count > 0)
            {
                this.POHeader.SetDefaultTransType(POTransactionType.GoodsReceived);
                this.TransHeaderProvider?.Items.Add(POHeader);
                this.TransHeaderProvider?.Update(this.CompId);
            }
        }
        #endregion TransactionReceipt Methods

        #region LotReceipt Update Methods
        protected virtual TransactionLotReceipt UpdateLotReceipt(TransactionReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry Number is required.");

            this.FilterEntityList(parent.LotReceiptList, ApiPoTransactionReceiptLotController.FunctionID);

            TransactionLotReceipt entity = parent.LotReceiptList.Find(x => x.EntryNum == Convert.ToInt32(bodyItem.EntryNum));
            if (entity == null)
                throw new InvalidValueException(string.Format("Entry Number '{0}' for Receipt Number '{1}' could not be found.", bodyItem.EntryNum, parent.ReceiptNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = LotUpdateComplete;
            entity.PropertyChanged += LotEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransactionLotReceipt CreateLotReceipt(TransactionReceipt parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry Number is required.");

            if (parent.LotReceiptList.Any(x => x.EntryNum == Convert.ToInt32(bodyItem.EntryNum)))
                throw new InvalidValueException(string.Format("Entry Number '{0}' already exists.", bodyItem.EntryNum));

            TransactionLotReceipt entity = parent.LotReceiptList.AddNew();
            entity.ReceiptId = Guid.NewGuid();

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

        protected virtual void LotUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransactionLotReceipt;
            entity.PropertyChanged -= LotEntity_PropertyChanged;
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
            var entity = sender as TransactionReceipt;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionReceipt> action = null;

            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);

            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void LotEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransactionLotReceipt> action = null;
            if (LotPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionLotReceipt);
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

        protected TransactionHeaderProvider TransHeaderProvider { get; } = new TransactionHeaderProvider();

        protected TransactionHeader POHeader
        {
            get
            {
                if (this._header == null)
                    this._header = EntityProvider.GetEntity<TransactionHeader, TransactionHeaderProvider>(new string[] { this.TransId }, this.CompId, null);

                return this._header;
            }
        }

        protected string TransId { get; set; }

        protected SortedDictionary<string, Action<TransactionReceipt>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionReceipt>>();

        protected SortedDictionary<string, Action<TransactionLotReceipt>> LotPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionLotReceipt>>();

        protected SortedDictionary<string, Action<TransactionSerial>> SerialPropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "9C002741-779C-40B3-8717-EB310A04187A";

        private TransactionHeader _header;
        #endregion Fields
    }
}
