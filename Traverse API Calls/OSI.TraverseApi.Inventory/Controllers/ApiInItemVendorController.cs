#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.Sys;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Inventory.Controllers
{
    public class ApiInItemVendorController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationid}/vendor/{id?}", typeof(ItemVendor))]
        public async Task<IHttpActionResult> Get(string itemId = null, string locationId = null, string id = null)
        {
            return Ok(await Load(itemId, locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationid}/vendor/{id?}", typeof(ItemVendor))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId = null, string locationId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationid}/vendor/{id?}", typeof(ItemVendor))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string locationId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, locationId, id));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationid}/vendor/{id}", typeof(ItemVendor))]
        public async Task Delete(string itemId, string locationId, string id)
        {
            await this.MarkToDelete(itemId, locationId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        { }

        protected virtual async Task Load(string id)
        {
            if (CurrentItem != null && string.Equals(CurrentItem.ItemId, id, StringComparison.OrdinalIgnoreCase))
                return;

            SqlFilterBuilder<ItemBase.Columns> builder = new SqlFilterBuilder<ItemBase.Columns>();
            builder.AppendEquals(ItemBase.Columns.ItemId, id);

            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            await this.FilterEntityListAsync(Provider.Items, ApiInItemController.FunctionID);
            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("Item '{0}' could not be found.", id));

            CurrentItem = Provider.Items[0];
        }

        protected virtual async Task Load(string itemId, string id)
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
                await this.FilterEntityListAsync(list, ApiInItemLocationController.FunctionID);
            }

            CurrentItem.CurrentLocation = CurrentItem.GetLocationById(id);
            if (CurrentItem.CurrentLocation == null)
                throw new InvalidValueException(string.Format("Location '{0}' cannot be found on item '{1}'", id, itemId));
        }

        protected virtual async Task<EntityList<ItemVendor>> Load(string itemId, string locationId, string id)
        {
            var list = CurrentItem?.CurrentLocation?.Vendors;

            if (CurrentItem?.CurrentLocation == null ||
                !StringHelper.AreEqual(CurrentItem?.ItemId ?? string.Empty, itemId, false) ||
                !StringHelper.AreEqual(CurrentItem?.CurrentLocation?.LocId ?? string.Empty, locationId, false))
            {
                await Load(itemId, locationId);

                list = CurrentItem.CurrentLocation.Vendors;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                list = list.FindAll(ItemVendorBase.Columns.VendId, id, true);

            return list;
        }

        protected virtual async Task<ItemVendor> Find(string itemId, string locationId, string id)
        {
            var list = await Load(itemId, locationId, id);
            return list.Find(x => StringHelper.AreEqual(x.VendId, id, false));
        }

        protected virtual async Task<List<ItemVendor>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string locationId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. Vendor ID is provided along with more than one record.");

            var entityList = new List<ItemVendor>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, locationId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ItemVendor> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string locationId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.VendId) || string.IsNullOrWhiteSpace(bodyItem.VendId))
                bodyItem.VendId = code;
            else
                code = bodyItem.VendId;

            var entity = await this.Find(itemId, locationId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                Vendor vendor = EntityProvider.GetEntity<Vendor, VendorProvider>(new string[] { code }, CompId, null);
                entity = CurrentItem.CurrentLocation.Vendors.AddNew();
                entity.VendName = vendor.Name;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Vendor '{0}' could not be found at location '{1}' on item '{2}'", code, locationId, itemId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string locationId, string id)
        {
            var entity = await this.Find(itemId, locationId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Vendor '{0}' could not be found at location '{1}' on item '{2}'", id, locationId, itemId));

            CurrentItem.CurrentLocation.Vendors.Remove(entity);
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
            Action<ItemVendor> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemVendor);
        }
        #endregion Event Handlers

        #region Properties
        protected ItemProvider Provider { get; } = new ItemProvider();

        protected Item CurrentItem { get; set; }

        protected SortedDictionary<string, Action<ItemVendor>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ItemVendor>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "20D03BE8-45B3-4CFD-9C92-AED2163AE355";
        #endregion Properties
    }
}
