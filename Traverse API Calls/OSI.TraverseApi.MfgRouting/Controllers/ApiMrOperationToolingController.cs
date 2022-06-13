#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Manufacturing.Controllers
{
    public class ApiMrOperationToolingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "operation/{operationid}/tooling/{id?}", typeof(OperationsTooling))]
        public async Task<IHttpActionResult> Get(string operationId, string id = null)
        {
            return Ok(await Load(operationId, id));
        }

        [ApiRoute(FunctionID, 2f, "operation/{operationid}/tooling/{id?}", typeof(OperationsTooling))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string operationId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, operationId, id));
        }

        [ApiRoute(FunctionID, 2f, "operation/{operationid}/tooling/{id}", typeof(OperationsTooling))]
        public async Task Delete(string operationId, string id)
        {
            await this.MarkToDelete(operationId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<OperationsTooling>> Load(string operationId, string id)
        {
            var list = this.CurrentOperation?.ToolingList;

            if (this.CurrentOperation == null || !StringHelper.AreEqual(CurrentOperation.OperationId, operationId, false))
            {
                var builder = new SqlFilterBuilder<OperationsBase.Columns>();
                builder.AppendEquals(OperationsBase.Columns.OperationId, operationId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Operation ID '{0}' could not be found.", operationId));

                await this.FilterEntityListAsync(Provider.Items, ApiMrOperationController.FunctionID);

                this.CurrentOperation = Provider.Items[0];

                list = this.CurrentOperation.ToolingList;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(OperationsToolingBase.Columns.ToolingId, id);

            return list;
        }

        protected virtual async Task<OperationsTooling> Find(string operationId, string id)
        {
            var list = await Load(operationId, id);
            return list.Find(x => StringHelper.AreEqual(x.ToolingId, id, false));
        }

        protected virtual async Task<List<OperationsTooling>> ProcessEditRequest(bool isCreate, dynamic body, string operationId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Tooling ID is provided along with more than one record.");

            var entityList = new List<OperationsTooling>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, operationId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<OperationsTooling> ProcessBodyItem(bool isCreate, dynamic bodyItem, string operationId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.ToolingId) || string.IsNullOrWhiteSpace(bodyItem.ToolingId))
                bodyItem.ToolingId = code;
            else
                code = bodyItem.ToolingId;

            var entity = await this.Find(operationId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentOperation?.ToolingList.AddNew();
                entity.SetDefaults();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string operationId, string id)
        {
            var entity = await this.Find(operationId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Tooling ID {0} could not be found for Operation ID '{1}.'", id, operationId));

            CurrentOperation.ToolingList.Remove(entity);
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
            Action<OperationsTooling> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as OperationsTooling);
        }
        #endregion Event Handlers

        #region Properties
        protected OperationsProvider Provider { get; } = new OperationsProvider();

        protected Operations CurrentOperation { get; set; }

        protected SortedDictionary<string, Action<OperationsTooling>> PropertyDictionary { get; } = new SortedDictionary<string, Action<OperationsTooling>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "386a4813-2030-48af-83b8-8ee5c1de9674";
        #endregion Fields
    }
}
