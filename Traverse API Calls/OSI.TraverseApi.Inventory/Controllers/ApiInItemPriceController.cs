#region Using Directives
using OSI.TraverseApi.Business;
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
    public class ApiInItemPriceController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationId}/unit/{id?}", typeof(ItemPrice))]
        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationId}/unit/{id?}/currency/{currencyid?}", typeof(ItemPrice))]
        public async Task<IHttpActionResult> Get(string itemId, string locationId, string id = null, string currencyId = null)
        {
            return Ok(await Load(itemId, locationId, id, currencyId));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationId}/unit/{id?}", typeof(ItemPrice))]
        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationId}/unit/{id?}/currency/{currencyid?}", typeof(ItemPrice))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId, string locationId, string id = null, string currencyId = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, locationId, id, currencyId));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationId}/unit/{id?}", typeof(ItemPrice))]
        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationId}/unit/{id?}/currency/{currencyid?}", typeof(ItemPrice))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId, string locationId, string id = null, string currencyId = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, locationId, id, currencyId));
        }

        [ApiRoute(FunctionID, 2f, "item/{itemId}/location/{locationId}/unit/{id?}/currency/{currencyid}", typeof(ItemPrice))]
        public async Task Delete(string itemId, string locationId, string id, string currencyId)
        {
            await this.MarkToDelete(itemId, locationId, id, currencyId);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        { }

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

        protected virtual async Task<EntityList<ItemPrice>> Load(string itemId, string locationId, string id, string currencyId)
        {
            var list = CurrentItem?.CurrentLocation?.Prices;

            if (CurrentItem?.CurrentLocation == null || 
                !StringHelper.AreEqual(CurrentItem?.ItemId ?? string.Empty, itemId, false) ||
                !StringHelper.AreEqual(CurrentItem?.CurrentLocation?.LocId ?? string.Empty, locationId, false))
            {
                await Load(itemId, locationId);

                list = CurrentItem.CurrentLocation.Prices;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                list = list.FindAll(ItemPriceBase.Columns.Uom, id, true);

            if (!string.IsNullOrEmpty(currencyId))
                list = list.FindAll(ItemPriceBase.Columns.CurrencyId, currencyId, true);

            return list;
        }

        protected virtual async Task<ItemPrice> Find(string itemId, string locationId, string id, string currencyId)
        {
            var list = await Load(itemId, locationId, id, currencyId);

            if (list.Count == 1 && (string.IsNullOrEmpty(currencyId) || StringHelper.AreEqual(currencyId, list[0].CurrencyId, false)))
                return list[0];
            else if (list.Count > 1)
                return list.Find(x => StringHelper.AreEqual(x.CurrencyId, string.IsNullOrEmpty(currencyId) ? DefaultCurrency : currencyId, false));
            return null;
        }

        protected virtual async Task<List<ItemPrice>> ProcessEditRequest(bool isCreate, dynamic body, string itemId, string locationId, string id = null, string currencyId = null)
        {
            DefaultCurrency = ConfigurationValue.GetRule<string>(AppId.SM, ConfigurationValue.BaseCurrency, this.CompId);

            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(currencyId))
                throw new InvalidValueException("Call is ambiguous. UOM/Currency is provided along with more than one record.");

            var entityList = new List<ItemPrice>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, locationId, id, currencyId);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ItemPrice> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string locationId, string id, string currencyId)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom) || string.IsNullOrWhiteSpace(bodyItem.Uom))
                bodyItem.Uom = code;
            else
                code = bodyItem.Uom;

            string currency = currencyId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.CurrencyId) || !string.IsNullOrWhiteSpace(bodyItem.CurrencyId))
                bodyItem.CurrencyId = currency;
            else
                currency = bodyItem.CurrencyId;

            var entity = await this.Find(itemId, locationId, code, currency);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CurrentItem.CurrentLocation.Prices.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("UOM '{0}' could not be found at location '{1}' on item '{2}'", code, locationId, itemId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string locationId, string id, string currencyId)
        {
            var entity = await this.Find(itemId, locationId, id, currencyId);

            if (entity == null)
                throw new InvalidValueException(string.Format("UOM '{0}' could not be found at location '{1}' on item '{2}'", id, locationId, itemId));

            CurrentItem.CurrentLocation.Prices.Remove(entity);
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
            Action<ItemPrice> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemPrice);
        }
        #endregion Event Handlers

        #region Properties
        protected ItemProvider Provider { get; } = new ItemProvider();

        protected Item CurrentItem { get; set; }

        protected string DefaultCurrency { get; set; }

        protected SortedDictionary<string, Action<ItemPrice>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ItemPrice>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private const string FunctionID = "5472F400-C9C8-475D-980D-FD17FC7ADC21";
        #endregion Properties
    }
}
