#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMrLaborTypeEmployeeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "labortype/{labortypeid}/employee/{id?}", typeof(LaborTypeEmployee))]
        public async Task<IHttpActionResult> Get(string laborTypeId, string id = null)
        {
            return Ok(await Load(laborTypeId, id));
        }

        [ApiRoute(FunctionID, 2f, "labortype/{labortypeid}/employee/{id?}", typeof(LaborTypeEmployee))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string laborTypeId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, laborTypeId, id));
        }

        [ApiRoute(FunctionID, 2f, "labortype/{labortypeid}/employee/{id}", typeof(LaborTypeEmployee))]
        public async Task Delete(string laborTypeId, string id)
        {
            await this.MarkToDelete(laborTypeId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<LaborTypeEmployee>> Load(string laborTypeId, string id)
        {
            var list = this.CurrentLabor?.EmployeeList;

            if (this.CurrentLabor == null || !StringHelper.AreEqual(CurrentLabor.LaborTypeId, laborTypeId, false))
            {
                var builder = new SqlFilterBuilder<LaborBase.Columns>();
                builder.AppendEquals(LaborBase.Columns.LaborTypeId, laborTypeId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Labor Type ID '{0}' could not be found.", laborTypeId));

                await this.FilterEntityListAsync(Provider.Items, ApiMrLaborTypeController.FunctionID);

                this.CurrentLabor = Provider.Items[0];

                list = this.CurrentLabor.EmployeeList;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(LaborTypeEmployeeBase.Columns.EmployeeId, id);

            return list;
        }

        protected virtual async Task<LaborTypeEmployee> Find(string laborTypeId, string id)
        {
            var list = await Load(laborTypeId, id);
            return list.Find(x => StringHelper.AreEqual(x.EmployeeId, id, false));
        }

        protected virtual async Task<List<LaborTypeEmployee>> ProcessEditRequest(bool isCreate, dynamic body, string laborTypeId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Employee ID is provided along with more than one record.");

            var entityList = new List<LaborTypeEmployee>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, laborTypeId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<LaborTypeEmployee> ProcessBodyItem(bool isCreate, dynamic bodyItem, string laborTypeId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EmployeeId) || string.IsNullOrWhiteSpace(bodyItem.EmployeeId))
                bodyItem.EmployeeId = code;
            else
                code = bodyItem.EmployeeId;

            var entity = await this.Find(laborTypeId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentLabor?.EmployeeList.AddNew();
                entity.SetDefaults();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string laborTypeId, string id)
        {
            var entity = await this.Find(laborTypeId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Employee ID {0} could not be found for Labor Type ID '{1}.'", id, laborTypeId));

            CurrentLabor.EmployeeList.Remove(entity);
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
            Action<LaborTypeEmployee> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as LaborTypeEmployee);
        }
        #endregion Event Handlers

        #region Properties
        protected LaborProvider Provider { get; } = new LaborProvider();

        protected Labor CurrentLabor { get; set; }

        protected SortedDictionary<string, Action<LaborTypeEmployee>> PropertyDictionary { get; } = new SortedDictionary<string, Action<LaborTypeEmployee>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "224ced3f-aee6-4442-a388-43633fc86c79";
        #endregion Fields
    }
}
