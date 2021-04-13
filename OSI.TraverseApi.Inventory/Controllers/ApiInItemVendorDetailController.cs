#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Inventory.Controllers
{
    public class ApiInItemVendorDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}/unit/{uom?}", typeof(VendorItemUomCost))]
        public async Task<IHttpActionResult> Get(string itemId, string vendorId, string uom = null)
        {
            return Ok(await Load(itemId, vendorId, uom));
        }

        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}/unit/{uom?}", typeof(VendorItemUomCost))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId, string vendorId, string uom = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, vendorId, uom));
        }

        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}/unit/{uom?}", typeof(VendorItemUomCost))]   
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId, string vendorId, string uom = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, vendorId, uom));
        }

        [ApiRoute(FunctionID, 2f, "itemvendor/item/{itemid}/vendor/{vendorid}/unit/{uom}", typeof(VendorItemUomCost))]
        public async Task Delete(string itemId, string vendorId, string uom)
        {
            await MarkToDelete(itemId, vendorId, uom);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<VendorItemUomCost>> Load(string itemId, string vendorId, string uom)
        {
            var list = CurrentItem?.VendorItemUomCostList;

            if (CurrentItem == null || CurrentItem.ItemId != itemId || CurrentItem.VendorId != vendorId)
            {
                var builder = new SqlFilterBuilder<VendorItemBase.Columns>();
                builder.AppendEquals(VendorItemBase.Columns.ItemId, itemId);
                builder.AppendEquals(VendorItemBase.Columns.VendorId, vendorId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiInItemVendorSetupController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Vendor ID '{0}' could not be found in Item ID '{1}'.", vendorId, itemId));

                CurrentItem = Provider.Items[0];

                list = CurrentItem.VendorItemUomCostList;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(uom))
                return list.FindAll(VendorItemUomCostBase.Columns.Uom, uom, true);

            return list;
        }

        protected virtual async Task<VendorItemUomCost> Find(string itemId, string vendorId, string uom)
        {
            var list = await Load(itemId, vendorId, uom);
            return list.Find(x => StringHelper.AreEqual(x.Uom, uom, false));
        }

        protected virtual async Task<List<VendorItemUomCost>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string vendorId, string uom)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(uom))
                throw new InvalidValueException("Call is ambiguous. Unit is provided along with more than one record.");

            var entityList = new List<VendorItemUomCost>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, vendorId, uom);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<VendorItemUomCost> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string vendorId, string uom)
        {
            string code = uom;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom) || string.IsNullOrWhiteSpace(bodyItem.Uom))
                bodyItem.Uom = code;
            else
                code = bodyItem.Uom;

            var entity = await this.Find(itemId, vendorId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CurrentItem.VendorItemUomCostList.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Unit '{0}' could not be found for Vendor ID '{1}' in Item ID '{2}'.", code, vendorId, itemId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string vendorId, string uom)
        {
            var entity = await this.Find(itemId, vendorId, uom);

            if (entity == null)
                throw new InvalidValueException(string.Format("Unit '{0}' could not be found for Vendor ID '{1}' in Item ID '{2}'.", uom, vendorId, itemId));

            CurrentItem.VendorItemUomCostList.Remove(entity);
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
            Action<VendorItemUomCost> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as VendorItemUomCost);
        }
        #endregion Event Handlers

        #region Properties
        protected VendorItemProvider Provider { get; } = new VendorItemProvider();

        protected VendorItem CurrentItem { get; set; }

        protected SortedDictionary<string, Action<VendorItemUomCost>> PropertyDictionary { get; } = new SortedDictionary<string, Action<VendorItemUomCost>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "2CB5E090-5D0A-4F0D-B107-3414AF42E341";
        #endregion Properties
    }
}
