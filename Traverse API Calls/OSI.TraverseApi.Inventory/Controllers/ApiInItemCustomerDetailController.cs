#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Inventory.Controllers
{
    public class ApiInItemCustomerDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custId}/unit/{uom?}", typeof(ItemCustDetail))]
        public async Task<IHttpActionResult> Get(string itemId, string custId, string uom = null)
        {
            return Ok(await Load(itemId, custId, uom));
        }

        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custId}/unit/{uom?}", typeof(ItemCustDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId, string custId, string uom = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, custId, uom));
        }

        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custId}/unit/{uom?}", typeof(ItemCustDetail))]   
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId, string custId, string uom = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, custId, uom));
        }

        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custId}/unit/{uom}", typeof(ItemCustDetail))]
        public async Task Delete(string itemId, string custId, string uom)
        {
            await MarkToDelete(itemId, custId, uom);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<ItemCustDetail>> Load(string itemId, string custId, string uom)
        {
            var list = CurrentItem?.ItemCustDetailList;

            if (CurrentItem == null || CurrentItem.ItemId != itemId || CurrentItem.CustId != custId)
            {
                var builder = new SqlFilterBuilder<ItemCustHeaderBase.Columns>();
                builder.AppendEquals(ItemCustHeaderBase.Columns.ItemId, itemId);
                builder.AppendEquals(ItemCustHeaderBase.Columns.CustId, custId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiInItemCustomerSetupController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Customer ID '{0}' could not be found in Item ID '{1}'.", custId, itemId));

                CurrentItem = Provider.Items[0];

                list = CurrentItem.ItemCustDetailList;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(uom))
                return list.FindAll(ItemCustDetailBase.Columns.Uom, uom, true);

            return list;
        }

        protected virtual async Task<ItemCustDetail> Find(string itemId, string custId, string uom)
        {
            var list = await Load(itemId, custId, uom);
            return list.Find(x => StringHelper.AreEqual(x.Uom, uom, false));
        }

        protected virtual async Task<List<ItemCustDetail>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string custId, string uom)
        {
            object[] list;
            uom = (StringHelper.AreEqual(uom, "undefined") || StringHelper.AreEqual(uom, "{uom}")) ? null : uom;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(uom))
                throw new InvalidValueException("Call is ambiguous. Unit is provided along with more than one record.");

            var entityList = new List<ItemCustDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, custId, uom);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ItemCustDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string custId, string uom)
        {
            string code = uom;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom) || string.IsNullOrWhiteSpace(bodyItem.Uom))
                bodyItem.Uom = code;
            else
                code = bodyItem.Uom;

            var entity = await this.Find(itemId, custId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;
                entity = CurrentItem.ItemCustDetailList.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Unit '{0}' could not be found for Customer ID '{1}' in Item ID '{2}'.", code, custId, itemId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string custId, string uom)
        {
            var entity = await this.Find(itemId, custId, uom);

            if (entity == null)
                throw new InvalidValueException(string.Format("Unit '{0}' could not be found for Customer ID '{1}' in Item ID '{2}'.", uom, custId, itemId));

            CurrentItem.ItemCustDetailList.Remove(entity);
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
            Action<ItemCustDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemCustDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected ItemCustHeaderProvider Provider { get; } = new ItemCustHeaderProvider();

        protected ItemCustHeader CurrentItem { get; set; }

        protected SortedDictionary<string, Action<ItemCustDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ItemCustDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "9761abbe-3725-4549-85b2-37e26a9cb9ce";
        #endregion Properties
    }
}
