#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Inventory.Controllers
{
    public class ApiInItemVendorSetupController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}", typeof(VendorItem))]
        [ApiRoute(FunctionID, 2f, "itemvendor/vendor/{vendorid}", typeof(VendorItem))]
        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}", typeof(VendorItem))]
        public async Task<IHttpActionResult> Get(string itemId = null, string vendorId = null)
        {
            return Ok(await this.Load(false, itemId, vendorId));
        }

        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}", typeof(VendorItem))]
        [ApiRoute(FunctionID, 2f, "itemvendor/vendor/{vendorid}", typeof(VendorItem))]
        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}", typeof(VendorItem))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId = null, string vendorId = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, vendorId));
        }

        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}", typeof(VendorItem))]
        [ApiRoute(FunctionID, 2f, "itemvendor/vendor/{vendorid}", typeof(VendorItem))]
        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}", typeof(VendorItem))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string vendorId = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, vendorId));
        }

        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}", typeof(VendorItem))]
        public async Task Delete(string itemId, string vendorId)
        {
            await this.MarkToDelete(itemId, vendorId);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            PropertyDictionary.Add(VendorItemBase.Columns.ItemId.ToString(), OnItemIdpdated);
        }

        protected virtual async Task<EntityList<VendorItem>> Load(bool isCreate, string itemId, string vendorId)
        {
            if (Provider.Items.Count <= 0 
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.ItemId, itemId, false)) 
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.VendorId, vendorId, false)))
            {
                var builder = new SqlFilterBuilder<VendorItemBase.Columns>();

                if (!string.IsNullOrEmpty(itemId))
                    builder.AppendEquals(VendorItemBase.Columns.ItemId, itemId);
                if (!string.IsNullOrEmpty(vendorId))
                    builder.AppendEquals(VendorItemBase.Columns.VendorId, vendorId);

                var list = new VendorItemProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                Provider.Items.AddRange(list);

                if (Provider.Items.Count <= 0 && !isCreate)
                {
                    if (string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(vendorId))
                        throw new NothingToProcessException(string.Format("Vendor ID '{0}' could not be found.", vendorId));

                    if (!string.IsNullOrEmpty(itemId) && string.IsNullOrEmpty(vendorId))
                        throw new NothingToProcessException(string.Format("Item ID '{0}' could not be found.", itemId));

                    else
                        throw new NothingToProcessException(string.Format("Vendor ID '{0}' with Item ID '{1}' could not be found.", vendorId, itemId));
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<VendorItem> Find(bool isCreate, string itemId, string vendorId)
        {
            var list = await Load(isCreate, itemId, vendorId);
            return list.Find(x => StringHelper.AreEqual(x.ItemId, itemId, false) && StringHelper.AreEqual(x.VendorId, vendorId, false));
        }

        protected virtual async Task<List<VendorItem>> ProcessEditRequest(bool isCreate, dynamic body, string itemId = null, string vendorId = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(vendorId)))
                throw new InvalidValueException("Call is ambiguous. Item ID and Vendor ID are provided along with more than one record.");

            var entityList = new List<VendorItem>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, vendorId);
                if (isCreate) this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<VendorItem> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string vendorId)
        {
            string item = itemId;
            string vendor = vendorId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId) || string.IsNullOrWhiteSpace(bodyItem.ItemId))
                bodyItem.ItemId = item;
            else
                item = bodyItem.ItemId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.VendorId) || string.IsNullOrWhiteSpace(bodyItem.VendorId))
                bodyItem.VendorId = vendor;
            else
                vendor = bodyItem.VendorId;

            var entity = await this.Find(isCreate, item, vendor);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new VendorItem(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Vendor ID '{0}' with Item ID '{1}' could not be found.", vendor, item));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string vendorId)
        {
            var entity = await this.Find(false, itemId, vendorId);

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            var item = args.ParentObject as VendorItem;

            if (item != null)
            {
                if (args.PropertyName == "VendorItemUomCostList")
                {
                    VendorItemUomCost entity = item.IsNew ?
                        this.CreateDetail(item, args.ItemModel) :
                        this.UpdateDetail(item, args.ItemModel);

                    if (entity == null)
                        args.Ignore = true;
                    else
                    {
                        ((ApiEntityModel)args.ItemModel).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
                        ((ApiEntityModel)args.ItemModel).FieldUpdateIsComplete = DetailUpdateComplete;
                        entity.PropertyChanged += DetailEntity_PropertyChanged;

                        Request.RegisterForDispose(entity);
                    }

                    Request.RegisterForDispose((ApiEntityModel)args.ItemModel);

                    return entity;
                }
            }

            return null;
        }

        protected virtual VendorItemUomCost UpdateDetail(VendorItem parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom))
                throw new InvalidValueException("Unit is required.");

            this.FilterEntityList(parent.VendorItemUomCostList, ApiInItemVendorDetailController.FunctionID);

            var entity = parent.VendorItemUomCostList.Find(x => StringHelper.AreEqual(x.Uom, bodyItem.Uom, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Unit '{0}' could not be found on Vendor ID '{1}' with Item ID '{2}'.", 
                    bodyItem.Uom, parent.VendorId, parent.ItemId));

            return entity;
        }

        protected virtual VendorItemUomCost CreateDetail(VendorItem parent, dynamic bodyItem)
        {
            var entity = parent.VendorItemUomCostList.AddNew();
            entity.SetDefaults();

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as VendorItemUomCost;
            entity.PropertyChanged -= DetailEntity_PropertyChanged;
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
            Action<VendorItem> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as VendorItem);
        }

        private void DetailEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<VendorItemUomCost> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as VendorItemUomCost);
        }

        protected virtual void OnItemIdpdated(VendorItem entity)
        {
            if (!string.IsNullOrEmpty(entity.ItemId))
                entity.SetDefaultUom();
        }
        #endregion Event Handlers

        #region Properties
        protected VendorItemProvider Provider { get; } = new VendorItemProvider();

        protected SortedDictionary<string, Action<VendorItem>> PropertyDictionary { get; } = new SortedDictionary<string, Action<VendorItem>>();

        protected SortedDictionary<string, Action<VendorItemUomCost>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<VendorItemUomCost>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "0CFC125A-377A-45EF-A0DF-B749E1D2DC00";
        #endregion Properties
    }
}
