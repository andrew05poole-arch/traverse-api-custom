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
    public class ApiInItemCustomerSetupController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}", typeof(ItemCustHeader))]
        [ApiRoute(FunctionID, 2f, "itemcustomer/customer/{custid}", typeof(ItemCustHeader))]
        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custid}", typeof(ItemCustHeader))]
        public async Task<IHttpActionResult> Get(string itemId = null, string custid = null)
        {
            return Ok(await this.Load(false, itemId, custid));
        }
        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}", typeof(ItemCustHeader))]
        [ApiRoute(FunctionID, 2f, "itemcustomer/customer/{custid}", typeof(ItemCustHeader))]
        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custid}", typeof(ItemCustHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string itemId = null, string custid = null)
        {
            return Ok(await ProcessEditRequest(false, body, itemId, custid));
        }

        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}", typeof(ItemCustHeader))]
        [ApiRoute(FunctionID, 2f, "itemcustomer/customer/{custid}", typeof(ItemCustHeader))]
        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custid}", typeof(ItemCustHeader))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string itemId = null, string custid = null)
        {
            return Ok(await ProcessEditRequest(true, body, itemId, custid));
        }
        [ApiRoute(FunctionID, 2f, "itemcustomer/item/{itemid}/customer/{custid}", typeof(ItemCustHeader))]
        public async Task Delete(string itemId, string custid)
        {
            await this.MarkToDelete(itemId, custid);
        }

        #endregion

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            PropertyDictionary.Add(ItemCustHeaderBase.Columns.ItemId.ToString(), OnItemIdpdated);
        }

        protected virtual async Task<EntityList<ItemCustHeader>> Load(bool isCreate, string itemId, string custid)
        {
            if (Provider.Items.Count <= 0 
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.ItemId, itemId, false)) 
                || !Provider.Items.Exists(x => StringHelper.AreEqual(x.CustId, custid, false)))
            {
                var builder = new SqlFilterBuilder<ItemCustHeaderBase.Columns>();

                if (!string.IsNullOrEmpty(itemId))
                    builder.AppendEquals(ItemCustHeaderBase.Columns.ItemId, itemId);
                if (!string.IsNullOrEmpty(custid))
                    builder.AppendEquals(ItemCustHeaderBase.Columns.CustId, custid);

                var list = new ItemCustHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                Provider.Items.AddRange(list);

                if (Provider.Items.Count <= 0 && !isCreate)
                {
                    if (string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(custid))
                        throw new NothingToProcessException(string.Format("Customer ID '{0}' could not be found.", custid));

                    if (!string.IsNullOrEmpty(itemId) && string.IsNullOrEmpty(custid))
                        throw new NothingToProcessException(string.Format("Item ID '{0}' could not be found.", itemId));

                    else
                        throw new NothingToProcessException(string.Format("Customer ID '{0}' with Item ID '{1}' could not be found.", custid, itemId));
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<ItemCustHeader> Find(bool isCreate, string itemId, string custid)
        {
            var list = await Load(isCreate, itemId, custid);
            return list.Find(x => StringHelper.AreEqual(x.ItemId, itemId, false) && StringHelper.AreEqual(x.CustId, custid, false));
        }

        protected virtual async Task<List<ItemCustHeader>> ProcessEditRequest(bool isCreate, dynamic body, string itemId = null, string customerId = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(customerId)))
                throw new InvalidValueException("Call is ambiguous. Item ID and Customer ID are provided along with more than one record.");

            var entityList = new List<ItemCustHeader>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, itemId, customerId);
                if (isCreate) this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ItemCustHeader> ProcessBodyItem(bool isCreate, dynamic bodyItem, string itemId, string customerId)
        {
            string item = itemId;
            string customer = customerId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ItemId) || string.IsNullOrWhiteSpace(bodyItem.ItemId))
                bodyItem.ItemId = item;
            else
                item = bodyItem.ItemId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.CustId) || string.IsNullOrWhiteSpace(bodyItem.CustId))
                bodyItem.CustId = customer;
            else
                customer = bodyItem.CustId;

            var entity = await this.Find(isCreate, item, customer);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new ItemCustHeader(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Customer ID '{0}' with Item ID '{1}' could not be found.", customer, item));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string itemId, string customerId)
        {
            var entity = await this.Find(false, itemId, customerId);

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            var item = args.ParentObject as ItemCustHeader;

            if (item != null)
            {
                if (args.PropertyName == "ItemCustDetailList")
                {
                    ItemCustDetail entity = item.IsNew ?
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

        protected virtual ItemCustDetail UpdateDetail(ItemCustHeader parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Uom))
                throw new InvalidValueException("Unit is required.");

            this.FilterEntityList(parent.ItemCustDetailList, ApiInItemCustomerDetailController.FunctionID);

            var entity = parent.ItemCustDetailList.Find(x => StringHelper.AreEqual(x.Uom, bodyItem.Uom, false));
            if (entity == null)
                throw new InvalidValueException(string.Format("Unit '{0}' could not be found on Customer ID '{1}' with Item ID '{2}'.", 
                    bodyItem.Uom, parent.CustId, parent.ItemId));

            return entity;
        }

        protected virtual ItemCustDetail CreateDetail(ItemCustHeader parent, dynamic bodyItem)
        {
            var entity = parent.ItemCustDetailList.AddNew();
            entity.SetDefaults();

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as ItemCustDetail;
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
            Action<ItemCustHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemCustHeader);
        }

        private void DetailEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ItemCustDetail> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ItemCustDetail);
        }

        protected virtual void OnItemIdpdated(ItemCustHeader entity)
        {
            if (!string.IsNullOrEmpty(entity.ItemId))
                entity.SetDefaultUom();
        }
        #endregion Event Handlers

        #region Properties
        protected ItemCustHeaderProvider Provider { get; } = new ItemCustHeaderProvider();

        protected SortedDictionary<string, Action<ItemCustHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ItemCustHeader>>();

        protected SortedDictionary<string, Action<ItemCustDetail>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<ItemCustDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "e4b99971-ab2c-4c39-bb0d-a79893db9d10";
       
        #endregion Properties
    }
}
