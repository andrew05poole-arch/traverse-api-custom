#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.AccountsReceivable.Controllers
{
    public class ApiArSalesAccountController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "salesaccount/{id?}", typeof(SalesAccount))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "salesaccount/{id?}", typeof(SalesAccount))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "salesaccount/{id?}", typeof(SalesAccount))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "salesaccount/{id}", typeof(SalesAccount))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<SalesAccount>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.AcctCode, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<SalesAccount>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<SalesAccountBase.Columns>();
                    builder.AppendEquals(SalesAccountBase.Columns.AcctCode, id);
                    var list = new SalesAccountProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        private async Task<SalesAccount> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.AcctCode, id, false));
        }

        protected virtual async Task<List<SalesAccount>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Account Code is provided along with more than one record.");

            var entityList = new List<SalesAccount>();
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

        protected virtual async Task<SalesAccount> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.AcctCode) || string.IsNullOrWhiteSpace(bodyItem.AcctCode))
                bodyItem.AcctCode = code;
            else
                code = bodyItem.AcctCode;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new SalesAccount(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Account Code '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Account Code '{0}' could not be found.", id));
            
            this.Provider.Items.Remove(entity);
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
            Action<SalesAccount> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as SalesAccount);
        }
        #endregion Event Handlers

        #region Properties
        protected SalesAccountProvider Provider { get; } = new SalesAccountProvider();

        protected SortedDictionary<string, Action<SalesAccount>> PropertyDictionary { get; } = new SortedDictionary<string, Action<SalesAccount>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "F5CF655C-43BE-4354-958E-A488EE349D84";
        #endregion Fields
    }
}
