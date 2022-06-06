#region Using Directives

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
    public class ApiSoCustomerLevelController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "customerlevel/{custlevel?}", typeof(CustomerLevel))]
        public async Task<IHttpActionResult> Get(string custLevel = null)
        {
            return Ok(await this.Load(custLevel));
        }

        [ApiRoute(FunctionID, 2f, "customerlevel/{custlevel?}", typeof(CustomerLevel))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string custLevel = null)
        {
            return Ok(await ProcessEditRequest(false, body, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "customerlevel/{custlevel?}", typeof(CustomerLevel))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string custLevel = null)
        {
            return Ok(await ProcessEditRequest(true, body, custLevel));
        }

        [ApiRoute(FunctionID, 2f, "customerlevel/{custlevel}", typeof(CustomerLevel))]
        public async Task Delete(string custLevel)
        {
            await this.MarkToDelete(custLevel);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<CustomerLevel>> Load(string custLevel)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(custLevel) && !Provider.Items.Exists(i => StringHelper.AreEqual(custLevel, i.CustLevel, false))))
            {
                if (string.IsNullOrEmpty(custLevel))
                    Provider.Load(this.CompId);
                else
                {
                    var builder = new SqlFilterBuilder<CustomerLevelBase.Columns>();
                    builder.AppendEquals(CustomerLevelBase.Columns.CustLevel, custLevel);
                    var list = new CustomerLevelProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<CustomerLevel> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.CustLevel, id, false));
        }

        protected virtual async Task<List<CustomerLevel>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Customer Level is provided along with more than one record.");

            var entityList = new List<CustomerLevel>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<CustomerLevel> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.CustLevel) || string.IsNullOrWhiteSpace(bodyItem.CustLevel))
                bodyItem.ResCode = code;
            else
                code = bodyItem.CustLevel;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new CustomerLevel(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Customer Level '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;
            entity.PropertyChanged -= Entity_PropertyChanged;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Customer Level '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        private void ValidateEntity(CustomerLevel entity)
        {
            if (!entity.ValidateAll(true))
            {
                if (entity.BrokenRulesList.Count > 0)
                {
                    throw new InvalidValueException(string.Format("The value for property {0} is not valid. Detail: {1}",
                        entity.BrokenRulesList[0].Property, entity.BrokenRulesList[0].Description));
                }
            }
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
            Action<CustomerLevel> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as CustomerLevel);
        }
        #endregion Event Handlers

        #region Properties
        private CustomerLevelProvider Provider { get; } = new CustomerLevelProvider();

        protected SortedDictionary<string, Action<CustomerLevel>> PropertyDictionary { get; } = new SortedDictionary<string, Action<CustomerLevel>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private const string FunctionID = "D4349775-2929-4BF5-ADAF-45E0BC3E6EBE";
        #endregion Properties
    }
}
