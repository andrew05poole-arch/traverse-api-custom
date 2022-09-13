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
    public class ApiMrWorkCenterMachineGroupController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "workcenter/{workcenterid}/machinegroup/{id?}", typeof(WCMachineGroups))]
        public async Task<IHttpActionResult> Get(string workCenterId, string id = null)
        {
            return Ok(await Load(workCenterId, id));
        }

        [ApiRoute(FunctionID, 2f, "workcenter/{workcenterid}/machinegroup/{id?}", typeof(WCMachineGroups))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string workCenterId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, workCenterId, id));
        }

        [ApiRoute(FunctionID, 2f, "workcenter/{workcenterid}/machinegroup/{id}", typeof(WCMachineGroups))]
        public async Task Delete(string workCenterId, string id)
        {
            await this.MarkToDelete(workCenterId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<WCMachineGroups>> Load(string workCenterId, string id)
        {
            var list = this.CurrentWorkCenter?.MachineGroupList;

            if (this.CurrentWorkCenter == null || !StringHelper.AreEqual(CurrentWorkCenter.WorkCenterId, workCenterId, false))
            {
                var builder = new SqlFilterBuilder<WorkCenterBase.Columns>();
                builder.AppendEquals(WorkCenterBase.Columns.WorkCenterId, workCenterId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Work Center ID '{0}' could not be found.", workCenterId));

                await this.FilterEntityListAsync(Provider.Items, ApiMrWorkCenterController.FunctionID);

                this.CurrentWorkCenter = Provider.Items[0];

                list = this.CurrentWorkCenter.MachineGroupList;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(WCMachineGroupsBase.Columns.MachineGroupId, id);

            return list;
        }

        protected virtual async Task<WCMachineGroups> Find(string workCenterId, string id)
        {
            var list = await Load(workCenterId, id);
            return list.Find(x => StringHelper.AreEqual(x.MachineGroupId, id, false));
        }

        protected virtual async Task<List<WCMachineGroups>> ProcessEditRequest(bool isCreate, dynamic body, string workCenterId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Machine Group ID is provided along with more than one record.");

            var entityList = new List<WCMachineGroups>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, workCenterId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<WCMachineGroups> ProcessBodyItem(bool isCreate, dynamic bodyItem, string workCenterId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.MachineGroupId) || string.IsNullOrWhiteSpace(bodyItem.MachineGroupId))
                bodyItem.MachineGroupId = code;
            else
                code = bodyItem.MachineGroupId;

            var entity = await this.Find(workCenterId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentWorkCenter?.MachineGroupList.AddNew();
                entity.SetDefaults();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string workCenterId, string id)
        {
            var entity = await this.Find(workCenterId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Machine Group ID {0} could not be found for Work Center ID '{1}.'", id, workCenterId));

            this.CurrentWorkCenter.MachineGroupList.Remove(entity);
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
            Action<WCMachineGroups> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as WCMachineGroups);
        }
        #endregion Event Handlers

        #region Properties
        protected WorkCenterProvider Provider { get; } = new WorkCenterProvider();

        protected WorkCenter CurrentWorkCenter { get; set; }

        protected SortedDictionary<string, Action<WCMachineGroups>> PropertyDictionary { get; } = new SortedDictionary<string, Action<WCMachineGroups>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "b3734f51-da1e-48c2-808d-ef8bf8ac0a1f";
        #endregion Fields
    }
}
