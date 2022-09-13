#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives 

namespace TRAVERSE.Web.API.Contacts.Controllers
{
    public class ApiArCustomersShipToController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "customer/{customerid}/shipto/{id?}", typeof(CustomerShipTo))]
        public async Task<IHttpActionResult> Get(string customerid, string id = null)
        {
            return Ok(await this.Load(customerid, id));
        }

        [ApiRoute(FunctionID, 2f, "customer/{customerid}/shipto/{id?}", typeof(CustomerShipTo))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string customerid, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, customerid, id));
        }

        [ApiRoute(FunctionID, 2f, "customer/{customerid}/shipto/{id?}", typeof(CustomerShipTo))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string customerid, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, customerid, id));
        }

        [ApiRoute(FunctionID, 2f, "customer/{customerid}/shipto/{id}", typeof(CustomerShipTo))]
        public async Task Delete(string customerid, string id)
        {
            await this.MarkToDelete(customerid, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //No special property changed events to set
        }

        protected virtual async Task<EntityList<CustomerShipTo>> Load(string customerId, string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(x => StringHelper.AreEqual(x.CustId, customerId, false) && StringHelper.AreEqual(x.ShiptoId, id, false))))
            {
                var builder = new SqlFilterBuilder<CustomerShipTo.Columns>();
                if (string.IsNullOrEmpty(id))
                {
                    builder.AppendEquals(CustomerShipTo.Columns.CustId, customerId);
                    await Provider.Load<CustomerShipTo>(this.CompId, new FilterCriteria(builder.ToString(), ""), PageNumber, PageSize);
                }
                else
                {
                    builder.AppendEquals(CustomerShipTo.Columns.CustId, customerId);
                    builder.AppendEquals(CustomerShipTo.Columns.ShiptoId, id);
                    var list = new CustomerShipToProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<CustomerShipTo> Find(string customerId, string id)
        {
            var list = await Load(customerId, id);
            return list.Find(x => StringHelper.AreEqual(x.CustId, customerId, false) && StringHelper.AreEqual(x.ShiptoId, id, false));
        }

        protected virtual async Task<List<CustomerShipTo>> ProcessEditRequest(bool isCreate, dynamic body, string customerId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Ship to ID is provided along with more than one record.");

            var entityList = new List<CustomerShipTo>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, customerId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<CustomerShipTo> ProcessBodyItem(bool isCreate, dynamic bodyItem, string customerId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ShiptoId) || string.IsNullOrWhiteSpace(bodyItem.ShiptoId))
                bodyItem.ShiptoId = code;
            else
                code = bodyItem.ShiptoId;

            var entity = await this.Find(customerId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new CustomerShipTo(this.CompId);
                entity.CustId = customerId;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Ship To '{0}' for customer '{1}' could not be found.", code, customerId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string customerId, string id)
        {
            var entity = await this.Find(customerId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Ship To '{0}' for customer '{1}' could not be found.", id, customerId));

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
            Action<CustomerShipTo> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as CustomerShipTo);
        }
        #endregion Event Handlers

        #region Properties
        protected CustomerShipToProvider Provider { get; } = new CustomerShipToProvider();

        protected SortedDictionary<string, Action<CustomerShipTo>> PropertyDictionary { get; } = new SortedDictionary<string, Action<CustomerShipTo>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "4FE336C1-83D6-4128-8A07-14327F26C240";
        #endregion Fields
    }
}

