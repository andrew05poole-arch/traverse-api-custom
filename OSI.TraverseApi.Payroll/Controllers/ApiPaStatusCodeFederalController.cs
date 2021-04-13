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
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaStatusCodeFederalController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "statuscode/federal/{id?}", typeof(FedStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/federal/{id?}", typeof(FedStatusCode))]
        public async Task<IHttpActionResult> Get(short? paYear = null, string id = null)
        {
            return Ok(await this.Load(paYear ?? PayrollContext.PayrollYear, id));
        }

        [ApiRoute(FunctionID, 2f, "statuscode/federal/{id?}", typeof(FedStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/federal/{id?}", typeof(FedStatusCode))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, short? paYear = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, paYear, id));
        }

        [ApiRoute(FunctionID, 2f, "statuscode/federal/{id?}", typeof(FedStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/federal/{id?}", typeof(FedStatusCode))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, short? paYear = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, paYear, id));
        }

        [ApiRoute(FunctionID, 2f, "statuscode/federal/{id}", typeof(FedStatusCode))]
        [ApiRoute(FunctionID, 2f, "statuscode/year/{payear:int}/federal/{id}", typeof(FedStatusCode))]
        public async Task Delete(string id, short? paYear = null)
        {
            await this.MarkToDelete(paYear ?? PayrollContext.PayrollYear, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<FedStatusCode>> Load(short paYear, string id)
        {
            if (this.Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(id, i.StatusCode, false))))
            {
                var builder = new SqlFilterBuilder<FedStatusCodeBase.Columns>();
                builder.AppendEquals(FedStatusCodeBase.Columns.PaYear, paYear.ToString());

                if (!string.IsNullOrEmpty(id))
                    builder.AppendEquals(FedStatusCodeBase.Columns.StatusCode, id);

                var list = new FedStatusCodeProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                this.Provider.Items.AddRange(list);

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }

            return Provider.Items;
        }

        protected virtual async Task<FedStatusCode> Find(short paYear, string id)
        {
            var list = await Load(paYear, id);
            return list?.Find(x => StringHelper.AreEqual(x.StatusCode, id, false));
        }

        protected virtual async Task<List<FedStatusCode>> ProcessEditRequest(bool isCreate, dynamic body, short? paYear, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Status Code is provided along with more than one record.");

            var entityList = new List<FedStatusCode>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, paYear, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<FedStatusCode> ProcessBodyItem(bool isCreate, dynamic bodyItem, short? paYear, string id)
        {
            string code = id;
            short currentPaYear = paYear ?? PayrollContext.PayrollYear;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.StatusCode) || string.IsNullOrWhiteSpace(bodyItem.StatusCode))
                bodyItem.StatusCode = code;
            else
                code = bodyItem.StatusCode;

            var entity = await this.Find(currentPaYear, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new FedStatusCode(this.CompId);
                entity.PaYear = currentPaYear;

                if (StringHelper.IsLessThanOrEqual((PayrollContext.CurrentPayrollNumber(entity.PaYear)).ToString(), "0"))
                    throw new InvalidValueException(string.Format("Payroll Year '{0}' is not valid.", entity.PaYear));
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Status Code '{0}' for Payroll Year '{1}' could not be found.", code, currentPaYear));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(short paYear, string id)
        {
            var entity = await this.Find(paYear, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Status Code '{0}' for Payroll Year '{1}' could not be found.", id, paYear));

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
            Action<FedStatusCode> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as FedStatusCode);
        }
        #endregion Event Handlers

        #region Properties
        protected FedStatusCodeProvider Provider { get; } = new FedStatusCodeProvider();

        protected SortedDictionary<string, Action<FedStatusCode>> PropertyDictionary { get; } = new SortedDictionary<string, Action<FedStatusCode>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "84E805E7-F613-43A9-8406-2AFFF8C2444A";
        #endregion Fields
    }
}
