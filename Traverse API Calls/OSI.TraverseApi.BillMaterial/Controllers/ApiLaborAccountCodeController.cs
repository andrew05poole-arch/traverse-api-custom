#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.BillMaterial;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.BillMaterial.Controllers
{
    public class ApiLaborAccountCodeController : ApiControllerBase
    {        
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "laborcode/{id?}", typeof(AccountCode))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "laborcode/{id?}", typeof(AccountCode))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "laborcode/{id?}", typeof(AccountCode))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "laborcode/{id?}", typeof(AccountCode))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<AccountCode>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.GLAcctCode, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<AccountCode>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<AccountCodeBase.Columns>();
                    builder.AppendEquals(AccountCodeBase.Columns.GLAcctCode, id);
                    var list = new AccountCodeProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<AccountCode> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.GLAcctCode, id, false));
        }

        protected virtual async Task<List<AccountCode>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous.Labor Account Code is provided along with more than one record.");

            var entityList = new List<AccountCode>();
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

        protected virtual async Task<AccountCode> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.GLAcctCode) || string.IsNullOrWhiteSpace(bodyItem.GLAcctCode))
                bodyItem.GLAcctCode = code;
            else
                code = bodyItem.GLAcctCode;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new AccountCode(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Labor Accout Code '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Labor Account Code'{0}' could not be found.", id));

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
            Action<AccountCode> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as AccountCode);
        }
        #endregion Event Handlers

        #region Properties
        protected AccountCodeProvider Provider { get; } = new AccountCodeProvider();

        protected SortedDictionary<string, Action<AccountCode>> PropertyDictionary { get; } = new SortedDictionary<string, Action<AccountCode>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "0CC89168-C4D8-40F9-8973-5907819DB1CF";
        #endregion Fields
    }
}