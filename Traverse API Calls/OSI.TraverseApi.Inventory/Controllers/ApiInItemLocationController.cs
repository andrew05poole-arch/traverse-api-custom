#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Inventory.Controllers
{
    public class ApiInItemLocationController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{id?}", typeof(ItemLocation))]
        public async Task<IHttpActionResult> Get(string itemId, string id = null)
        {
            return Ok(await Load(itemId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{id?}", typeof(ItemLocation))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{id?}", typeof(ItemLocation))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{id}", typeof(ItemLocation))]
        public async Task Delete(string itemId, string id)
        {
            await this.MarkToDelete(itemId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(ItemLocationBase.Columns.QtyOrderPoint.ToString(), 
                (entity) => entity.OrderQuantityOrderPointType = OrderQuantityCalculationType.Manual);
            PropertyDictionary.Add(ItemLocationBase.Columns.QtySafetyStock.ToString(), 
                (entity) => entity.OrderQuantitySafetyStockType = OrderQuantityCalculationType.Manual);
            PropertyDictionary.Add(ItemLocationBase.Columns.Eoq.ToString(),
                (entity) => entity.OrderQuantityEOQType = OrderQuantityCalculationType.Manual);
            PropertyDictionary.Add(ItemLocationBase.Columns.CurrencyIdACV.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 0));
            PropertyDictionary.Add(ItemLocationBase.Columns.CostAvg.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 1));
            PropertyDictionary.Add(ItemLocationBase.Columns.CostBase.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 2));
            PropertyDictionary.Add(ItemLocationBase.Columns.CostLandedLast.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 3));
            PropertyDictionary.Add(ItemLocationBase.Columns.CostLast.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 4));
            PropertyDictionary.Add(ItemLocationBase.Columns.CostStd.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 5));
        }

        protected virtual async Task<EntityList<ItemLocation>> Load(string itemId, string id)
        {
            var list = CurrentItem?.AllLocations;

            if (CurrentItem == null || !StringHelper.AreEqual(CurrentItem.ItemId, itemId, false))
            {
                var builder = new SqlFilterBuilder<ItemBase.Columns>();
                builder.AppendEquals(ItemBase.Columns.ItemId, itemId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiInItemController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Item '{0}' could not be found.", id));

                CurrentItem = Provider.Items[0];

                list = CurrentItem.AllLocations;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(ItemLocationBase.Columns.LocId, id, true);

            return list;
        }

        protected virtual async Task<ItemLocation> Find(string itemId, string id)
        {
            var list = await Load(itemId, id);
            return list.Find(x => StringHelper.AreEqual(x.LocId, id, false));
        }

        protected virtual async Task<List<ItemLocation>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string id = null)
        {
            DefaultCurrency = ConfigurationValue.GetRule<string>(AppId.SM, ConfigurationValue.BaseCurrency, this.CompId);

            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Location ID is provided along with more than one record.");

            var entityList = new List<ItemLocation>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ItemLocation> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId) || string.IsNullOrWhiteSpace(bodyItem.LocId))
                bodyItem.LocId = code;
            else
                code = bodyItem.LocId;

            var entity = await this.Find(itemId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CurrentItem.AllLocations.AddNew();
                entity.OrderQtyUom = CurrentItem.UomDflt;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Location '{0}' could not be found on item '{1}'.", code, itemId));

            CurrentItem.CurrentLocation = entity;

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string id)
        {
            var entity = await this.Find(itemId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Location '{0}' could not be found on item '{1}'.", id, itemId));

            CurrentItem.AllLocations.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            ItemLocation location = args.ParentObject as ItemLocation;

            CurrentItem.CurrentLocation = location;
            if (args.PropertyName == "Prices")
            {
                ItemPrice entity = location.IsNew ?
                    this.CreateItemLocationPrice(location, args.ItemModel) :
                    this.UpdateItemLocationPrice(location, args.ItemModel);

                ((ApiEntityModel)args.ItemModel).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
                ((ApiEntityModel)args.ItemModel).FieldUpdateIsComplete = PriceUpdateComplete;
                entity.PropertyChanged += ItemPrice_PropertyChanged;

                Request.RegisterForDispose(entity);
                Request.RegisterForDispose((ApiEntityModel)args.ItemModel);

                return entity;
            }
            if (args.PropertyName == "Vendors")
            {
                ItemVendor entity = location.IsNew ?
                    this.CreateItemLocationVendor(location, args.ItemModel) :
                    this.UpdateItemLocationVendor(location, args.ItemModel);

                ((ApiEntityModel)args.ItemModel).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
                ((ApiEntityModel)args.ItemModel).FieldUpdateIsComplete = VendorUpdateComplete;
                entity.PropertyChanged += ItemVendor_PropertyChanged;

                Request.RegisterForDispose(entity);
                Request.RegisterForDispose((ApiEntityModel)args.ItemModel);

                return entity;
            }

            return null;
        }

        private ItemPrice UpdateItemLocationPrice(ItemLocation parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom))
                throw new InvalidValueException("Unit is required.");

            string currency = ApiUserSkipped.IsApiUserSkipped(bodyItem.CurrencyId) ? null : bodyItem.CurrencyId;

            ItemPrice entity = null;
            var list = parent.Prices.FindAll(x => StringHelper.AreEqual(x.Uom, bodyItem.Uom, false));

            if (list.Count == 1 && (string.IsNullOrEmpty(currency) || StringHelper.AreEqual(currency, list[0].CurrencyId, false)))
                entity = list[0];
            else if (list.Count > 1)
                entity = list.Find(x => StringHelper.AreEqual(x.CurrencyId, string.IsNullOrEmpty(currency) ? DefaultCurrency : currency, false));

            if (entity == null)
                throw new InvalidValueException(string.Format("Price record for Unit '{0}' in Location '{1}' for Item ID '{2}' could not be found.",
                    bodyItem.Uom, parent.LocId, parent.ItemId));

            return entity;
        }

        private ItemPrice CreateItemLocationPrice(ItemLocation parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom))
                throw new InvalidValueException("Unit is required.");

            string currency = (ApiUserSkipped.IsApiUserSkipped(bodyItem.CurrencyId) ? null : bodyItem.CurrencyId) ?? DefaultCurrency;

            ItemPrice entity = parent.Prices.Find(x => StringHelper.AreEqual(x.Uom, bodyItem.Uom, false)
                && StringHelper.AreEqual(x.CurrencyId, currency, false));

            if (entity != null)
                throw new InvalidValueException(string.Format("Price record for Unit '{0}' in Location '{1}' for Item ID '{2}' already exists.",
                    bodyItem.Uom, parent.LocId, parent.ItemId));

            entity = parent.Prices.AddNew();
            entity.CurrencyId = DefaultCurrency;

            return entity;
        }

        private ItemVendor UpdateItemLocationVendor(ItemLocation parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.VendId))
                throw new InvalidValueException("Vendor ID is required.");

            ItemVendor entity = parent.Vendors.Find(ItemVendorBase.Columns.VendId, bodyItem.VendId);

            if (entity == null)
                throw new InvalidValueException(string.Format("Vendor ID '{0}' in Location '{1}' for Item ID '{2}' could not be found.",
                    bodyItem.VendId, parent.LocId, parent.ItemId));

            return entity;
        }

        private ItemVendor CreateItemLocationVendor(ItemLocation parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.VendId))
                throw new InvalidValueException("Vendor ID is required.");

            ItemVendor entity = parent.Vendors?.Find(ItemVendorBase.Columns.VendId, bodyItem.VendId);

            if (entity != null)
                throw new InvalidValueException(string.Format("Vendor ID '{0}' in Location '{1}' for Item ID '{2}' already exists.",
                    bodyItem.VendId, parent.LocId, parent.ItemId));
            else
                entity = parent.Vendors.AddNew();

            return entity;
        }

        protected virtual void PriceUpdateComplete(object entityObject)
        {
            var entity = entityObject as ItemPrice;
            entity.PropertyChanged -= ItemPrice_PropertyChanged;
        }

        protected virtual void VendorUpdateComplete(object entityObject)
        {
            var entity = entityObject as ItemVendor;
            entity.PropertyChanged -= ItemVendor_PropertyChanged;
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
            Action<ItemLocation> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemLocation);
        }

        private void ItemPrice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ItemPrice> action = null;
            if (PricePropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemPrice);
        }

        private void ItemVendor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ItemVendor> action = null;
            if (VendorPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemVendor);
        }
        #endregion Event Handlers

        #region Properties
        protected ItemProvider Provider { get; } = new ItemProvider();

        protected Item CurrentItem { get; set; }

        protected string DefaultCurrency { get; set; }

        protected SortedDictionary<string, Action<ItemLocation>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ItemLocation>>();

        protected SortedDictionary<string, Action<ItemPrice>> PricePropertyDictionary { get; } = new SortedDictionary<string, Action<ItemPrice>>();

        protected SortedDictionary<string, Action<ItemVendor>> VendorPropertyDictionary { get; } = new SortedDictionary<string, Action<ItemVendor>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "EB96F902-1AA7-4311-84FA-E7CE37866F07";
        #endregion Properties
    }
}
