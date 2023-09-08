using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoShipmentDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/shipmentDetail/{transId}/entrynum/{entryNum:int}", typeof(ShipmentDetail))]
        public async Task<IHttpActionResult> Get(string shipNum, string transId, int entryNum)
        {
            return Ok(await this.Load(shipNum, transId, entryNum, false));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/shipmentDetail/{transId}/entrynum/{entryNum:int}", typeof(ShipmentDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string shipNum, string transId, int entryNum)
        {
            return Ok(await ProcessEditRequest(false, body, shipNum, transId, entryNum));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/shipmentDetail", typeof(ShipmentDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string shipNum)
        {
            return Ok(await ProcessEditRequest(true, body, shipNum, null, null));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipNum}/shipmentDetail/{transId}/entryNum/{entryNum}", typeof(ShipmentDetail))]
        public async Task Delete(string shipNum, string transId, int entryNum)
        {
            await this.MarkToDelete(shipNum, transId, entryNum);
        }
        #endregion Web Methods

        #region Helper Methods        
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(ShipmentDetailBase.Columns.TransId.ToString(), ValidateTransId);
        }

        protected virtual void ValidateTransId(ShipmentDetail entity)
        {
            SqlFilterBuilder<TransactionHeader> builder = new SqlFilterBuilder<TransactionHeader>();

            builder.AppendEquals(TransactionHeaderBase.Columns.TransId.ToString(), entity.TransId);
            TransactionHeaderProvider provider = new TransactionHeaderProvider();
            provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));
            if (provider.Count > 0)
            {
                TransactionDetail transactionDetail = provider.Items[0].TransactionDetailList.Find(TransactionDetailBase.Columns.EntryNum, entity.EntryNum);
                if (transactionDetail != null)
                {
                    entity.VendorId = provider.Items[0].VendorId;
                    entity.ItemId = transactionDetail.ItemId;
                    entity.Description = transactionDetail.Description;
                    entity.LocId = transactionDetail.LocationId;
                    entity.Unit = transactionDetail.Unit;
                    entity.OrdQty = transactionDetail.QtyOrd;
                    entity.OrdUnitCost = transactionDetail.UnitCost;
                }
                else
                {
                    throw new InvalidValueException(String.Format("Trans ID '{0}' with Entry Num {1} is invalid.", entity.TransId, entity.EntryNum));
                }
            }
            else
            {
                throw new InvalidValueException(String.Format("Trans ID '{0}' is invalid.", entity.TransId));
            }
        }

        protected virtual async Task<EntityList<ShipmentDetail>> Load(string shipNum, string transId, int? entryNum, bool isCreate)
        {
            var list = CurrentShipmentHeader?.DetailList as EntityList<ShipmentDetail>;

            if (CurrentShipmentHeader == null || !this.Provider.Items.Exists(i => i.ShipNum == shipNum))
            {
                var builder = new SqlFilterBuilder<ShipmentHeaderBase.Columns>();
                builder.AppendEquals(ShipmentHeaderBase.Columns.ShipNum, shipNum.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPoShipmentController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Shipment Num '{0}' could not be found.", shipNum));

                CurrentShipmentHeader = Provider.Items[0];

                list = CurrentShipmentHeader.DetailList as EntityList<ShipmentDetail>;
                await this.FilterEntityListAsync(list);
            }

            if (transId != null && !string.IsNullOrEmpty(transId) && entryNum.HasValue && !isCreate)
            {
                list = list.FindAll(d => StringHelper.AreEqual(d.TransId, transId, false) && d.EntryNum == entryNum.Value);
                if (list.Count <= 0)
                {
                    throw new InvalidValueException(string.Format("Shipment Detail '{0}/{1}' could not be found on Shipment '{2}'.", transId, entryNum.Value, shipNum));
                }
                else
                    return list;
            }

            return list;
        }

        protected virtual async Task<ShipmentDetail> Find(string shipNum, string transId, int? entryNum, bool isCreate)
        {
            var list = await Load(shipNum, transId, entryNum, isCreate);
            return list.Find(x => StringHelper.AreEqual(x.TransId, transId, false) && x.EntryNum == entryNum.Value);
        }

        protected virtual async Task<List<ShipmentDetail>> ProcessEditRequest(bool isCreate, dynamic body, string shipNum, string transId, int? entryNum)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(transId) && entryNum.HasValue)
                throw new InvalidValueException("Call is ambiguous. Shipment Detail is provided along with more than one record.");

            var entityList = new List<ShipmentDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, shipNum, transId, entryNum);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ShipmentDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string shipNum, string transId, int? entryNum)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId) || string.IsNullOrWhiteSpace(bodyItem.TransId))
                bodyItem.TransId = transId;
            else
                transId = bodyItem.TransId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum) || bodyItem.EntryNum == null)
                bodyItem.EntryNum = entryNum.GetValueOrDefault();
            else
                entryNum = Convert.ToInt32(bodyItem.EntryNum);

            var entity = await this.Find(shipNum, transId, entryNum.GetValueOrDefault(), isCreate);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentShipmentHeader.DetailList.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Shipment Detail '{0}/{1}' could not be found on Shipment '{2}'.", transId, entryNum, shipNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string shipNum, string transId, int entryNum)
        {
            var entity = await this.Find(shipNum, transId, entryNum, false);

            if (entity == null)
                throw new InvalidValueException(string.Format("Shipment Detail '{0}/{1}' could not be found on Shipment '{2}'", transId, entryNum, shipNum));

            CurrentShipmentHeader.DetailList.Remove(entity);
            this.Provider.Update(this.CompId);
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
            Action<ShipmentDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ShipmentDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected ShipmentHeaderProvider Provider { get; } = new ShipmentHeaderProvider();

        protected ShipmentHeader CurrentShipmentHeader { get; set; }

        protected SortedDictionary<string, Action<ShipmentDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ShipmentDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "90f5291c-8729-4f65-bc0f-62ca7141ca9d";
        #endregion Fields
    }
}
