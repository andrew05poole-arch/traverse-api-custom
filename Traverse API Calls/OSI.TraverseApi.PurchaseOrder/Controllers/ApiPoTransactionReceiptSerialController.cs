#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class ApiPoTransactionReceiptSerialController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{entrynum:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> Get(string transId, string receiptNum, int entryNum, string id = null)
        {
            return Ok(await Load(transId, receiptNum, entryNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{entrynum:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string transId, string receiptNum, int entryNum, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, transId, receiptNum, entryNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{entrynum:int}/serial/{id?}", typeof(TransactionSerial))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string transId, string receiptNum, int entryNum, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, transId, receiptNum, entryNum, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{transid}/receipt/{receiptnum}/entrynum/{entrynum:int}/serial/{id}", typeof(TransactionSerial))]
        public async Task Delete(string transId, string receiptNum, int entryNum, string id)
        {
            await this.MarkToDelete(transId, receiptNum, entryNum, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //Recepit Serial Property Changed
            PropertyDictionary.Add(TransactionSerialBase.Columns.SerNum.ToString(), SerialNumberPropertyChanged);
            PropertyDictionary.Add(TransactionSerialBase.Columns.RcptUnitCostFgn.ToString(), (entity) =>
            {
                entity.SuppressEntityEvents = true;
                entity.CalculateReceiptBaseCost();
                entity.SuppressEntityEvents = false;
            });
        }

        protected virtual async Task Load(string transId, string receiptNum, int entryNum)
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

                    this.CurrentLotReceipt = this.CurrentReceipt.LotReceiptList.Find(x => x.EntryNum == entryNum);

                    if (this.CurrentLotReceipt == null)
                        throw new InvalidValueException("Entry Number could not be found.");
                }
                else
                    throw new InvalidValueException("Transaction ID could not be found.");
            }
        }

        protected virtual async Task<EntityList<TransactionSerial>> Load(string transId, string receiptNum, int entryNum, string id)
        {
            var list = this.CurrentLotReceipt?.SerialList as EntityList<TransactionSerial>;

            if (this.CurrentReceipt == null || !StringHelper.AreEqual(this.CurrentReceipt.TransId, transId, false)
                || !StringHelper.AreEqual(this.CurrentReceipt.ReceiptNum, receiptNum, false))
            {
                await Load(transId, receiptNum, entryNum);

                list = this.CurrentLotReceipt.SerialList as EntityList<TransactionSerial>;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                list = list?.FindAll(TransactionSerialBase.Columns.SerNum, id, true);

            return list;
        }

        protected virtual async Task<TransactionSerial> Find(string transId, string receiptNum, int entryNum, string id)
        {
            var list = await Load(transId, receiptNum, entryNum, id);
            return list?.Find(x => StringHelper.AreEqual(x.SerNum, id, false));
        }

        protected virtual async Task<List<TransactionSerial>> ProcessEditRequest(bool isCreate, dynamic body, string transId, string receiptNum, int entryNum, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Serial Number is provided along with more than one record.");

            var entityList = new List<TransactionSerial>();

            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, transId, receiptNum, entryNum, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionSerial> ProcessBodyItem(bool isCreate, dynamic bodyItem, string transId, string receiptNum, int entryNum, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SerNum) || string.IsNullOrEmpty(bodyItem.SerNum))
                bodyItem.SerNum = code;
            else
                code = bodyItem.SerNum;

            var entity = await this.Find(transId, receiptNum, entryNum, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = (this.CurrentLotReceipt?.SerialList as EntityList<TransactionSerial>).AddNew();
                entity.SetDefaultBin();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' could not be found on entry number '{1}', receipt number '{2}' and transaction '{3}'.", code, entryNum, receiptNum, transId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string transId, string receiptNum, int entryNum, string id)
        {
            var entity = await this.Find(transId, receiptNum, entryNum, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Serial number '{0}' could not be found on entry number '{1}', receipt number '{2}' and transaction '{3}'.", id, entryNum, receiptNum, transId));

            this.CurrentReceipt.LotReceiptList[0]?.SerialList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void SerialNumberPropertyChanged(TransactionSerial entity)
        {
            if (string.IsNullOrEmpty(entity.SerNum)
                || entity.TransactionLotReceipt?.TransactionDetail?.INItemInfo == null)
                return;

            if (entity.TransactionLotReceipt?.TransactionDetail?.INItemInfo.InventoryType != InventoryType.Serial)
                throw new InvalidValueException("Current item does not support adding serial numbers.");

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
            var entity = sender as TransactionSerial;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<TransactionSerial> action = null;

            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);

            entity.PropertyChanged += Entity_PropertyChanged;
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionReceiptProvider Provider { get; } = new TransactionReceiptProvider();

        protected TransactionReceipt CurrentReceipt { get; set; }

        protected TransactionLotReceipt CurrentLotReceipt { get; set; }

        protected SortedDictionary<string, Action<TransactionSerial>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionSerial>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "fe2dbdf4-628a-4260-9c84-51b5be4256cb";
        #endregion Fields
    }
}
