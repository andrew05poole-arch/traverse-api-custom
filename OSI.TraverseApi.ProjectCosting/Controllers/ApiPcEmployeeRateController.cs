#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TraverseApi;
using T = System.Threading.Tasks;
#endregion Using Directives

namespace OSI.TraverseApi.ProjectCosting.Controllers
{
    public class ApiPcEmployeeRateController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employeerate/{employeeid?}", typeof(ResourceRates))]
        [ApiRoute(FunctionID, 2f, "employeerate/{employeeid}/rate/{id?}", typeof(ResourceRates))]
        public async Task<IHttpActionResult> Get(string employeeId = null, string id = null)
        {
            return Ok(await Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employeerate/{employeeid?}", typeof(ResourceRates))]
        [ApiRoute(FunctionID, 2f, "employeerate/{employeeid}/rate/{id?}", typeof(ResourceRates))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employeerate/{employeeid?}", typeof(ResourceRates))]
        [ApiRoute(FunctionID, 2f, "employeerate/{employeeid}/rate/{id?}", typeof(ResourceRates))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId = null, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employeerate/{employeeid}/rate/{id}", typeof(ResourceRates))]
        public async T.Task Delete(string employeeId, string id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<ResourceRates>> Load(string employeeId, string id)
        {
            if (Provider.Items.Count <= 0 
                || (!string.IsNullOrEmpty(employeeId) && !Provider.Items.Exists(i => StringHelper.AreEqual(i.EmpId, employeeId, false)))
                || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(i.RateId, id, false))))
            {
                if (string.IsNullOrEmpty(employeeId))
                    await Provider.Load<ResourceRates>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<ResourceRatesBase.Columns>();
                    builder.AppendEquals(ResourceRatesBase.Columns.EmpId, employeeId);
                    if (!string.IsNullOrEmpty(id))
                        builder.AppendEquals(ResourceRatesBase.Columns.RateId, id);
                    var list = new ResourceRatesProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }
            return Provider.Items;
        }


        protected virtual async Task<ResourceRates> Find(string employeeId, string id)
        {
            var list = await Load(employeeId, id);
            return list.Find(x => StringHelper.AreEqual(x.EmpId, employeeId, false) && StringHelper.AreEqual(x.RateId, id, false));
        }

        protected virtual async Task<List<ResourceRates>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrWhiteSpace(id))
                throw new InvalidValueException("Call is ambiguous. Rate ID is provided along with more than one record.");

            var entityList = new List<ResourceRates>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, employeeId, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ResourceRates> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, string id)
        {
            string employee = employeeId;
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EmpId) || string.IsNullOrWhiteSpace(bodyItem.EmpId))
                bodyItem.EmpId = employee;
            else
                employee = bodyItem.EmpId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.RateId) || string.IsNullOrWhiteSpace(bodyItem.RateId))
                bodyItem.RateId = code;
            else
                code = bodyItem.RateId;

            var entity = await this.Find(employee, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new ResourceRates(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Employee ID '{0}' with Rate Code '{1}' could not be found.", employee, code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(string employeeId, string id)
        {
            var entity = await this.Find(employeeId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Employee ID '{0}' with Rate Code '{1}' could not be found.", employeeId, id));

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
            Action<ResourceRates> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ResourceRates);
        }
        #endregion Event Handlers

        #region Properties
        protected ResourceRatesProvider Provider { get; } = new ResourceRatesProvider();

        protected SortedDictionary<string, Action<ResourceRates>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ResourceRates>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "F1B3351E-5782-4437-9409-3B5434819990";
        #endregion Fields
    }
}
