#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.Inventory.Controllers
{
    public class ApiInItemController: ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "item/{id?}", typeof(Item))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "item/{id?}", typeof(Item))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{id?}", typeof(Item))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{id}", typeof(Item))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(ItemBase.Columns.ItemStatus.ToString(), ItemStatusPropertyChanged);

            LocationPropertyDictionary.Add(ItemLocationBase.Columns.QtyOrderPoint.ToString(),
                (entity) => entity.OrderQuantityOrderPointType = OrderQuantityCalculationType.Manual);
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.QtySafetyStock.ToString(),
                (entity) => entity.OrderQuantitySafetyStockType = OrderQuantityCalculationType.Manual);
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.Eoq.ToString(),
                (entity) => entity.OrderQuantityEOQType = OrderQuantityCalculationType.Manual);
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.CurrencyIdACV.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 0));
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.CostAvg.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 1));
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.CostBase.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 2));
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.CostLandedLast.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 3));
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.CostLast.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 4));
            LocationPropertyDictionary.Add(ItemLocationBase.Columns.CostStd.ToString(), (entity) => ItemUtility.CalculateACVValues(entity, 5));

            VendorPropertyDictionary.Add(ItemVendorBase.Columns.VendId.ToString(), (entity) => entity.VendName = entity.VendorInfo?.Name);
        }

        protected virtual async Task<EntityList<Item>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.ItemId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Item>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<ItemBase.Columns>();
                    builder.AppendEquals(ItemBase.Columns.ItemId, id);
                    var list = new ItemProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Item> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.ItemId, id, false));
        }

        protected virtual async Task<List<Item>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            DefaultCurrency = ConfigurationValue.GetRule<string>(AppId.SM, ConfigurationValue.BaseCurrency, this.CompId);

            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Item ID is provided along with more than one record.");

            var entityList = new List<Item>();
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

        protected virtual async Task<Item> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId) || string.IsNullOrWhiteSpace(bodyItem.ItemId))
                bodyItem.ItemId = code;
            else
                code = bodyItem.ItemId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Item(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Item '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Item '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            Item item = args.ParentObject as Item;
            ItemLocation location = args.ParentObject as ItemLocation;

            if (item != null)
            {
                if (args.PropertyName == "Units")
                {
                    ItemUnit entity = item.IsNew ?
                        this.CreateItemUnit(item, args.ItemModel) :
                        this.UpdateItemUnit(item, args.ItemModel);

                    if (entity == null)
                        args.Ignore = true;
                    else
                    {
                        ((ApiEntityModel)args.ItemModel).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
                        ((ApiEntityModel)args.ItemModel).FieldUpdateIsComplete = UnitUpdateComplete;
                        entity.PropertyChanged += ItemUnit_PropertyChanged;

                        Request.RegisterForDispose(entity);
                    }

                    Request.RegisterForDispose((ApiEntityModel)args.ItemModel);

                    return entity;
                }
                if (args.PropertyName == "AllLocations")
                {
                     location = item.IsNew ?
                        this.CreateItemLocation(item, args.ItemModel) :
                        this.UpdateItemLocation(item, args.ItemModel);

                    ((ApiEntityModel)args.ItemModel).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
                    ((ApiEntityModel)args.ItemModel).FieldUpdateIsComplete = LocationUpdateComplete;
                    location.PropertyChanged += ItemLocation_PropertyChanged;

                    Request.RegisterForDispose(location);
                    Request.RegisterForDispose((ApiEntityModel)args.ItemModel);

                    item.CurrentLocation = location;

                    return location;
                }
            }
            else if (location != null)
            {
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
            }

            return null;
        }

        private ItemUnit UpdateItemUnit(Item parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom))
                throw new InvalidValueException("Unit is required.");

            ItemUnit entity = parent.Units.Find(ItemUnitBase.Columns.Uom, bodyItem.Uom);
            if (entity == null)
                throw new InvalidValueException(string.Format("Unit '{0}' for Item ID '{1}' could not be found.", bodyItem.Uom, parent.ItemId));

            return entity;
        }

        private ItemUnit CreateItemUnit(Item parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom) || bodyItem.Uom == null)
            {
                if (parent.InventoryType == InventoryType.Serial)
                    return null;

                throw new InvalidValueException("Unit is required.");
            }

            if (parent.InventoryType == InventoryType.Serial && parent.Units.Count >= 1)
                throw new InvalidValueException(string.Format("Item '{0}' is seralized. Serialized items cannot have more than one unit of measure.", parent.ItemId));

            ItemUnit entity = parent.Units.Find(ItemUnitBase.Columns.Uom, bodyItem.Uom);
            if (entity != null)
                throw new InvalidValueException(string.Format("Unit '{0}' for Item ID '{1}' already exists.", bodyItem.Uom, parent.ItemId));

            entity = parent.Units.AddNew();

            return entity;
        }

        private ItemLocation UpdateItemLocation(Item parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId))
                throw new InvalidValueException("Location ID is required.");

            ItemLocation entity = parent.AllLocations.Find(ItemLocationBase.Columns.LocId, bodyItem.LocId);
            if (entity == null)
                throw new InvalidValueException(string.Format("Location ID '{0}' for Item ID '{1}' could not be found.", bodyItem.LocId, parent.ItemId));

            return entity;
        }

        private ItemLocation CreateItemLocation(Item parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId))
                throw new InvalidValueException("Location ID is required.");

            ItemLocation entity = parent.AllLocations?.Find(ItemLocationBase.Columns.LocId, bodyItem.LocId);
            if (entity != null)
                throw new InvalidValueException(string.Format("Location ID '{0}' for Item ID '{1}' already exists.", bodyItem.LocId, parent.ItemId));
            
            entity = parent.AllLocations.AddNew();
            entity.OrderQtyUom = parent.UomDflt;

            return entity;
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
            {
                Vendor vendor = EntityProvider.GetEntity<Vendor, VendorProvider>(new string[] { bodyItem.VendId }, CompId, null);
                entity = parent.Vendors.AddNew();
                entity.VendName = vendor.Name;
            }

            return entity;
        }

        protected virtual void ItemStatusPropertyChanged(Item entity)
        {
            if (entity.InventoryStatus != InventoryStatus.Superseded)
                entity.SuperId = string.Empty;

            if (entity.IsNew && entity.InventoryStatus != InventoryStatus.Active)
            {
                foreach (ItemLocation location in entity.AllLocations.FindAll(l => l.InventoryStatus == InventoryStatus.Active))
                    location.InventoryStatus = entity.InventoryStatus;
            }
        }

        protected virtual void UnitUpdateComplete(object entityObject)
        {
            var entity = entityObject as ItemUnit;
            entity.PropertyChanged -= ItemUnit_PropertyChanged;
        }

        protected virtual void LocationUpdateComplete(object entityObject)
        {
            var entity = entityObject as ItemLocation;
            entity.PropertyChanged -= ItemLocation_PropertyChanged;
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
            Action<Item> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Item);
        }

        private void ItemUnit_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ItemUnit> action = null;
            if (UnitPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemUnit);
        }

        private void ItemLocation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ItemLocation> action = null;
            if (LocationPropertyDictionary.TryGetValue(e.PropertyName, out action))
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

        protected string DefaultCurrency { get; set; }

        protected SortedDictionary<string, Action<Item>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Item>>();

        protected SortedDictionary<string, Action<ItemUnit>> UnitPropertyDictionary { get; } = new SortedDictionary<string, Action<ItemUnit>>();

        protected SortedDictionary<string, Action<ItemLocation>> LocationPropertyDictionary { get; } = new SortedDictionary<string, Action<ItemLocation>>();

        protected SortedDictionary<string, Action<ItemPrice>> PricePropertyDictionary { get; } = new SortedDictionary<string, Action<ItemPrice>>();

        protected SortedDictionary<string, Action<ItemVendor>> VendorPropertyDictionary { get; } = new SortedDictionary<string, Action<ItemVendor>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "4C906B6C-B5C9-48CE-86BA-85EDDE8D5950";
        #endregion Fields
    }
}
