#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Pricing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Pricing.Controllers
{
    public class ApiSoCustomerPricingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid?}", typeof(CustomerPricing))]
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid?}", typeof(CustomerPricing))]
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel?}", typeof(CustomerPricing))]
        public async Task<IHttpActionResult> Get(string itemId = null, string locId = null, string custLevel = null)
        {
            return Ok(await this.Load(itemId, locId, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid?}", typeof(CustomerPricing))]
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid?}", typeof(CustomerPricing))]
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel?}", typeof(CustomerPricing))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId = null, string locId = null, string custLevel = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, locId, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid?}", typeof(CustomerPricing))]
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid?}", typeof(CustomerPricing))]
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel?}", typeof(CustomerPricing))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string locId = null, string custLevel = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, locId, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel}", typeof(CustomerPricing))]
        public async Task Delete(string itemId = null, string locId = null, string custLevel = null)
        {
            await this.MarkToDelete(itemId, locId, custLevel);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<CustomerPricing>> Load(string itemId, string locId, string custLevel)
        {
            if (Provider.Items.Count <= 0 || !Provider.Items.Exists(i => 
                StringHelper.AreEqual(i.ItemId, itemId, false) && 
                (string.IsNullOrWhiteSpace(locId) || StringHelper.AreEqual(i.LocId, locId, false)) && 
                (string.IsNullOrWhiteSpace(custLevel) || StringHelper.AreEqual(i.CustLevel, custLevel, false))
                ))
            {
                if (string.IsNullOrWhiteSpace(itemId))
                    Provider.Load(this.CompId);
                else
                {
                    var builder = new SqlFilterBuilder<CustomerPricingBase.Columns>();
                    if (!string.IsNullOrEmpty(itemId))
                        builder.AppendEquals(CustomerPricingBase.Columns.ItemId, itemId);
                    if (!string.IsNullOrEmpty(locId))
                        builder.AppendEquals(CustomerPricingBase.Columns.LocId, locId);
                    if (!string.IsNullOrEmpty(custLevel))
                        builder.AppendEquals(CustomerPricingBase.Columns.CustLevel, custLevel);

                    var list = new CustomerPricingProvider().Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<CustomerPricing> Find(string itemId, string locId, string custLevel)
        {
            var list = await Load(itemId, locId, custLevel);
            return list.Find(x => StringHelper.AreEqual(x.ItemId, itemId, false) && StringHelper.AreEqual(x.LocId, locId, false) && StringHelper.AreEqual(x.CustLevel, custLevel, false));
        }

        protected virtual async Task<List<CustomerPricing>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string locId, string custLevel)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(itemId))
                throw new InvalidValueException("Call is ambiguous. Customer Pricing is provided along with more than one record.");

            var entityList = new List<CustomerPricing>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, locId, custLevel);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<CustomerPricing> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string locId, string custLevel)
        {
            string itemCode = itemId;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId) || string.IsNullOrWhiteSpace(bodyItem.ItemId))
                bodyItem.ItemId = itemCode;
            else
                itemCode = bodyItem.ItemId;

            string locCode = locId;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LocId) || string.IsNullOrWhiteSpace(bodyItem.LocId))
                bodyItem.LocId = locCode;
            else
                locCode = bodyItem.LocId;

            string lvlCode = custLevel;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.CustLevel) || string.IsNullOrWhiteSpace(bodyItem.CustLevel))
                bodyItem.CustLevel = lvlCode;
            else
                lvlCode = bodyItem.CustLevel;

            var entity = await this.Find(itemCode, locCode, lvlCode);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new CustomerPricing(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Customer Price for Item ID '{0}' with Location ID '{1}' and Customer Level '{2}' could not be found.",
                    itemCode, locCode, lvlCode));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;
            entity.PropertyChanged -= Entity_PropertyChanged;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string locId, string custLevel)
        {
            var entity = await this.Find(itemId, locId, custLevel);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Customer Price for Item ID '{0}' with Location ID '{1}' and Customer Level '{2}' could not be found.", 
                    itemId, locId, custLevel));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
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
            Action<CustomerPricing> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as CustomerPricing);
        }
        #endregion Event Handlers

        #region Properties
        protected CustomerPricingProvider Provider { get; } = new CustomerPricingProvider();

        protected SortedDictionary<string, Action<CustomerPricing>> PropertyDictionary { get; } = new SortedDictionary<string, Action<CustomerPricing>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "87BEE63E-09E3-4C59-9738-AD11E0DC2A18";
        #endregion Properties
    }
}
