#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Pricing;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Pricing.Controllers
{
    public class ApiSoCustomerPricingDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel}/detail/{detailid?}", typeof(ItemLocPriceDetail))]
        public async Task<IHttpActionResult> Get(string itemId = null, string locId = null, string custLevel = null, string detailId = null)
        {
            return Ok(await this.Load(itemId, locId, custLevel, detailId));
        }

        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel}/detail/{detailid?}", typeof(ItemLocPriceDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId = null, string locId = null, string custLevel = null, string detailId = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, locId, custLevel, detailId));
        }

        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel}/detail", typeof(ItemLocPriceDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string locId = null, string custLevel = null, string detailId = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, locId, custLevel, detailId));
        }

        [ApiRoute(FunctionID, 2f, "customerpricing/item/{itemid}/location/{locid}/customerlevel/{custlevel}/detail/{detailid}", typeof(ItemLocPriceDetail))]
        public async Task Delete(string itemId = null, string locId = null, string custLevel = null, string detailId = null)
        {
            await this.MarkToDelete(itemId, locId, custLevel, detailId);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<CustomerPricing> Find(string itemId, string locId, string custLevel)
        {
            var customerPricing = this.Provider.Items.Find(x => StringHelper.AreEqual(x.ItemId, itemId, false) && StringHelper.AreEqual(x.LocId, locId, false)
                && StringHelper.AreEqual(x.CustLevel, custLevel, false));

            if (customerPricing == null)
            {
                customerPricing = EntityProvider.GetEntity<CustomerPricing, CustomerPricingProvider>(new string[] { itemId, locId, custLevel }, this.CompId, null);
                if (customerPricing != null)
                    this.Provider.Items.Add(customerPricing);

                await FilterEntityListAsync(Provider.Items, ApiSoCustomerPricingController.FunctionID);
                if (!Provider.Items.Contains(customerPricing))
                    customerPricing = null;
            }
            return customerPricing;
        }

        protected virtual async Task<EntityList<ItemLocPriceDetail>> Load(string itemId, string locId, string custLevel, string detailId)
        {
            var header = await Find(itemId, locId, custLevel);

            if (header == null)
                return new EntityList<ItemLocPriceDetail>();

            await this.FilterEntityListAsync(header.CustomerPricingDetailList);

            if (!string.IsNullOrEmpty(detailId))
            {
                return header.CustomerPricingDetailList.FindAll(x => StringHelper.AreEqual(x.Id.ToString(), detailId, false));
            }
            return header.CustomerPricingDetailList;
        }

        protected virtual async Task<ItemLocPriceDetail> Find(CustomerPricing header, string detailId)
        {
            if (header == null)
                return null;

            var list = header.CustomerPricingDetailList;
            await FilterEntityListAsync(list);
            return list.Find(x => StringHelper.AreEqual(x.Id.ToString(), detailId, false));
        }

        protected virtual async Task<List<ItemLocPriceDetail>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string locId, string custLevel, string detailId = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(detailId))
                throw new InvalidValueException("Call is ambiguous. Customer Pricing Detail is provided along with more than one record.");

            var entityList = new List<ItemLocPriceDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, locId, custLevel, detailId);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ItemLocPriceDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string locId, string custLevel, string detailId)
        {
            string code = detailId;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || string.IsNullOrWhiteSpace(Convert.ToString(bodyItem.Id)))
                bodyItem.Id = code;
            else
                code = Convert.ToString(bodyItem.Id);

            var header = await this.Find(itemId, locId, custLevel);
            var entity = await this.Find(header, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = header.CustomerPricingDetailList.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Customer Pricing '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;
            entity.PropertyChanged -= Entity_PropertyChanged;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string locId, string custLevel, string detailId)
        {
            var header = await this.Find(itemId, locId, custLevel);
            var entity = await this.Find(header, detailId);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Customer Pricing detail record '{0}' could not be found.", detailId));

            header.CustomerPricingDetailList.Remove(entity);
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
            Action<ItemLocPriceDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemLocPriceDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected CustomerPricingProvider Provider { get; } = new CustomerPricingProvider();

        protected SortedDictionary<string, Action<ItemLocPriceDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ItemLocPriceDetail>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "FC39876D-606A-4E61-A9A1-9546E36FD527";
        #endregion Properties
    }
}
