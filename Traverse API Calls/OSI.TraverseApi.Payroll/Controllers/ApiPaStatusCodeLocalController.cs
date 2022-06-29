#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Business.PayrollTax;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaStatusCodeLocalController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "statuscode/state/{statecode}/local/{localcode}/status/{id?}", typeof(LocalStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/state/{statecode}/local/{localcode}/status/{id?}", typeof(LocalStatusCode))]
        public async Task<IHttpActionResult> Get(string stateCode, string localCode, short? paYear = null, string id = null)
        {
            return Ok(await this.Load(paYear ?? PayrollContext.PayrollYear, stateCode, localCode, id));
        }

        [ApiRoute(FunctionID, 2f, "statuscode/state/{statecode}/local/{localcode}/status/{id?}", typeof(LocalStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/state/{statecode}/local/{localcode}/status/{id?}", typeof(LocalStatusCode))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string stateCode, string localCode, short? paYear = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, paYear, stateCode, localCode, id));
        }

        [ApiRoute(FunctionID, 2f, "statuscode/state/{statecode}/local/{localcode}/status/{id?}", typeof(LocalStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/state/{statecode}/local/{localcode}/status/{id?}", typeof(LocalStatusCode))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string stateCode, string localCode, short? paYear = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, paYear, stateCode, localCode, id));
        }

        [ApiRoute(FunctionID, 2f, "statuscode/state/{statecode}/local/{localcode}/status/{id}", typeof(LocalStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/state/{statecode}/local/{localcode}/status/{id}", typeof(LocalStatusCode))]
        public async Task Delete(string stateCode, string localCode, string id, short? paYear = null)
        {
            await this.MarkToDelete(paYear ?? PayrollContext.PayrollYear, stateCode, localCode, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<LocalStatusCode>> Load(short paYear, string stateCode, string localCode, string id)
        {
            if (this.Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(id, i.StatusCode, false))))
            {
                var builder = new SqlFilterBuilder<LocalStatusCodeBase.Columns>();
                builder.AppendEquals(LocalStatusCodeBase.Columns.PaYear, paYear.ToString());
                builder.AppendEquals(LocalStatusCodeBase.Columns.StateCode, stateCode);
                builder.AppendEquals(LocalStatusCodeBase.Columns.LocalCode, localCode);

                if (!string.IsNullOrEmpty(id))
                    builder.AppendEquals(LocalStatusCodeBase.Columns.StatusCode, id);

                var list = new LocalStatusCodeProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                this.Provider.Items.AddRange(list);

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }

            return Provider.Items;
        }

        protected virtual async Task<LocalStatusCode> Find(short paYear, string stateCode, string localCode, string id)
        {
            var list = await Load(paYear, stateCode, localCode, id);
            return list?.Find(x => StringHelper.AreEqual(x.StatusCode, id, false));
        }

        protected virtual async Task<List<LocalStatusCode>> ProcessEditRequest(bool isCreate, dynamic body, short? paYear, string stateCode, string localCode, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Status Code is provided along with more than one record.");

            var entityList = new List<LocalStatusCode>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, paYear, stateCode, localCode, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<LocalStatusCode> ProcessBodyItem(bool isCreate, dynamic bodyItem, short? paYear, string stateCode, string localCode, string id)
        {
            string code = id;
            short currentPaYear = paYear ?? PayrollContext.PayrollYear;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.StatusCode) || string.IsNullOrWhiteSpace(bodyItem.StatusCode))
                bodyItem.StatusCode = code;
            else
                code = bodyItem.StatusCode;

            var entity = await this.Find(currentPaYear, stateCode, localCode, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new LocalStatusCode(this.CompId);
                entity.PaYear = currentPaYear;
                entity.StateCode = stateCode;
                entity.LocalCode = localCode;

                if (StringHelper.IsLessThanOrEqual((PayrollContext.CurrentPayrollNumber(entity.PaYear)).ToString(), "0"))
                    throw new InvalidValueException(string.Format("Payroll Year '{0}' is not valid.", entity.PaYear));
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Status Code '{0}' for Payroll Year '{1}' in State Code '{2}' and Local Code '{3}' could not be found.", 
                    code, currentPaYear, stateCode, localCode));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(short paYear, string stateCode, string localCode, string id)
        {
            var entity = await this.Find(paYear, stateCode, localCode, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Status Code '{0}' for Payroll Year '{1}' in State Code '{2}' and Local Code '{3}' could not be found.", 
                    id, paYear, stateCode, localCode));

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
            Action<LocalStatusCode> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as LocalStatusCode);
        }
        #endregion Event Handlers

        #region Properties
        protected LocalStatusCodeProvider Provider { get; } = new LocalStatusCodeProvider();

        protected SortedDictionary<string, Action<LocalStatusCode>> PropertyDictionary { get; } = new SortedDictionary<string, Action<LocalStatusCode>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "555B872E-0B39-468F-9C48-E21CB118E37F";
        #endregion Fields
    }
}
