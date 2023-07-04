using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Business.API;
using System.ComponentModel;
using System.Net.Http;
using TRAVERSE.Business.Inventory;

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoShipmentController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "shipment/{shipnum}", typeof(ShipmentHeader))]
        public async Task<IHttpActionResult> Get(string shipnum)
        {
            return Ok(await this.Load(shipnum));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipnum?}", typeof(ShipmentHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string shipnum = null)
        {
            return Ok(await ProcessEditRequest(false, body, shipnum));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipnum?}", typeof(ShipmentHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string shipnum = null)
        {
            return Ok(await ProcessEditRequest(true, body, shipnum));
        }

        [ApiRoute(FunctionID, 2f, "shipment/{shipnum}", typeof(ShipmentHeader))]
        public async Task Delete(string shipnum)
        {
            await this.MarkToDelete(shipnum);
        }
        #endregion Web Methods

        #region Helper Methods

        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(ShipmentHeaderBase.Columns.Status.ToString(), SetCompletedDate);
        }

        protected virtual void SetCompletedDate(ShipmentHeader entity)
        {
            entity.SetCompletedDate();
        }

        protected virtual async Task<EntityList<ShipmentHeader>> Load(string shipnum)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(shipnum) && !Provider.Items.Exists(i => StringHelper.AreEqual(shipnum, i.ShipNum, false))))
            {
                if (string.IsNullOrEmpty(shipnum))
                    await Provider.Load<ShipmentHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<ShipmentHeader.Columns>();
                    builder.AppendEquals(ShipmentHeader.Columns.ShipNum, shipnum);
                    var list = new ShipmentHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<ShipmentHeader> Find(string shipnum)
        {
            var list = await Load(shipnum);
            return list.Find(x => StringHelper.AreEqual(x.ShipNum, shipnum, false));
        }

        protected virtual async Task<List<ShipmentHeader>> ProcessEditRequest(bool isCreate, dynamic body, string shipnum)
        {
            shipnum = (StringHelper.AreEqual(shipnum, "undefined") || StringHelper.AreEqual(shipnum, "{shipnum}")) ? null : shipnum;

            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(shipnum))
                throw new InvalidValueException("Call is ambiguous. Shipment No is provided along with more than one record.");

            var entityList = new List<ShipmentHeader>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, shipnum);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ShipmentHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string shipnum)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ShipNum) || string.IsNullOrWhiteSpace(bodyItem.ShipNum))
                bodyItem.ShipNum = shipnum;
            else
                shipnum = bodyItem.ShipNum;

            var entity = await this.Find(shipnum);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new ShipmentHeader(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Shipment No '{0}' could not be found.", shipnum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string shipnum)
        {
            var entity = await this.Find(shipnum);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Shipment No '{0}' could not be found.", shipnum));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailList")
            {
                if (((ShipmentHeader)args.ParentObject).IsNew)
                    return this.CreateShipmentDetail((ShipmentHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateShipmentDetail((ShipmentHeader)args.ParentObject, args.ItemModel);
            }
            else if (args.PropertyName == "LandedCostDetailList")
            {
                if (((ShipmentHeader)args.ParentObject).IsNew)
                    return this.CreateLandedCostDetail((ShipmentHeader)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateLandedCostDetail((ShipmentHeader)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Shipment Detail Method
        protected virtual ShipmentDetail UpdateShipmentDetail(ShipmentHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.HeaderId))
                throw new InvalidValueException("Shipment ID is required.");
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.TransId))
                throw new InvalidValueException("Transaction ID is required.");
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum))
                throw new InvalidValueException("Entry Num is required.");

            this.FilterEntityList(parent.DetailList, ApiPoShipmentDetailController.FunctionID);
            ShipmentDetail entity = parent?.DetailList?.Find(d => d.HeaderId == Convert.ToInt64(bodyItem.HeaderId)
            && StringHelper.AreEqual(d.TransId, bodyItem.TransId, false) && d.EntryNum == Convert.ToInt32(bodyItem.EntryNum));
            if (entity == null)
                throw new InvalidValueException(string.Format("Transaction ID {0} with Entry Num {1} could not be found on Shipment '{2}'", Convert.ToInt64(bodyItem.Id), bodyItem.EntryNum, parent.ShipNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ShipmentDetailUpdateComplete;
            entity.PropertyChanged += ShipmentDetailEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual ShipmentDetail CreateShipmentDetail(ShipmentHeader parent, dynamic bodyItem)
        {
            ShipmentDetail entity = parent.DetailList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ShipmentDetailUpdateComplete;
            entity.PropertyChanged += ShipmentDetailEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void ShipmentDetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as ShipmentDetail;
            entity.PropertyChanged -= ShipmentDetailEntity_PropertyChanged;
        }
        #endregion Shipment Detail Method

        #region Landed Cost Detail Method
        protected virtual ShipmentLandedCostDetail UpdateLandedCostDetail(ShipmentHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Shipment Landed Cost Detail ID is required.");

            this.FilterEntityList(parent.LandedCostDetailList, ApiPoShipmentLandedCostDetailController.FunctionID);
            ShipmentLandedCostDetail entity = parent?.LandedCostDetailList?.Find(ShipmentLandedCostDetailBase.Columns.Id, Convert.ToInt64(bodyItem.Id));
            if (entity == null)
                throw new InvalidValueException(string.Format("Shipment Landed Cost Detail ID '{0}' could not be found on Shipment '{1}'", bodyItem.Id, parent.ShipNum));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ShipmentLandedCostDetailUpdateComplete;
            entity.PropertyChanged += ShipmentLandedCostDetailEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual ShipmentLandedCostDetail CreateLandedCostDetail(ShipmentHeader parent, dynamic bodyItem)
        {
            ShipmentLandedCostDetail entity = parent.LandedCostDetailList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ShipmentLandedCostDetailUpdateComplete;
            entity.PropertyChanged += ShipmentLandedCostDetailEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void ShipmentLandedCostDetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as ShipmentLandedCostDetail;
            entity.PropertyChanged -= ShipmentLandedCostDetailEntity_PropertyChanged;
        }
        #endregion Landed Cost Detail Method

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
            Action<ShipmentHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ShipmentHeader);
        }

        private void ShipmentDetailEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ShipmentDetail> action = null;
            if (ShipmentDetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ShipmentDetail);
        }

        private void ShipmentLandedCostDetailEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ShipmentLandedCostDetail> action = null;
            if (ShipmentLandedCostDetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ShipmentLandedCostDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected ShipmentHeaderProvider Provider { get; } = new ShipmentHeaderProvider();

        protected SortedDictionary<string, Action<ShipmentHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ShipmentHeader>>();

        protected SortedDictionary<string, Action<ShipmentDetail>> ShipmentDetailPropertyDictionary { get; } = new SortedDictionary<string, Action<ShipmentDetail>>();

        protected SortedDictionary<string, Action<ShipmentLandedCostDetail>> ShipmentLandedCostDetailPropertyDictionary { get; } = new SortedDictionary<string, Action<ShipmentLandedCostDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "7dd68b20-7589-427a-9519-bfcef251204c";
        #endregion Fields
    }
}
