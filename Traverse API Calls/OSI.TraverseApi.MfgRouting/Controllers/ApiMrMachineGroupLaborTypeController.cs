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
    public class ApiMrMachineGroupLaborTypeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "machinegroup/{machinegroupid}/labortype/{id?}", typeof(MachineLabor))]
        public async Task<IHttpActionResult> Get(string machineGroupId, string id = null)
        {
            return Ok(await Load(machineGroupId, id));
        }

        [ApiRoute(FunctionID, 2f, "machinegroup/{machinegroupid}/labortype/{id?}", typeof(MachineLabor))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string machineGroupId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, machineGroupId, id));
        }

        [ApiRoute(FunctionID, 2f, "machinegroup/{machinegroupid}/labortype/{id}", typeof(MachineLabor))]
        public async Task Delete(string machineGroupId, string id)
        {
            await this.MarkToDelete(machineGroupId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<MachineLabor>> Load(string machineGroupId, string id)
        {
            var list = this.CurrentMachineGroups?.LaborTypeList;

            if (this.CurrentMachineGroups == null || !StringHelper.AreEqual(CurrentMachineGroups.MachineGroupId, machineGroupId, false))
            {
                var builder = new SqlFilterBuilder<MachineGroupsBase.Columns>();
                builder.AppendEquals(MachineGroupsBase.Columns.MachineGroupId, machineGroupId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Machine Group ID '{0}' could not be found.", machineGroupId));

                await this.FilterEntityListAsync(Provider.Items, ApiMrMachineGroupController.FunctionID);

                this.CurrentMachineGroups = Provider.Items[0];

                list = this.CurrentMachineGroups.LaborTypeList;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(MachineLaborBase.Columns.LaborTypeId, id);

            return list;
        }

        protected virtual async Task<MachineLabor> Find(string machineGroupId, string id)
        {
            var list = await Load(machineGroupId, id);
            return list.Find(x => StringHelper.AreEqual(x.LaborTypeId, id, false));
        }

        protected virtual async Task<List<MachineLabor>> ProcessEditRequest(bool isCreate, dynamic body, string machineGroupId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Labor Type ID is provided along with more than one record.");

            var entityList = new List<MachineLabor>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, machineGroupId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<MachineLabor> ProcessBodyItem(bool isCreate, dynamic bodyItem, string machineGroupId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborTypeId) || string.IsNullOrWhiteSpace(bodyItem.LaborTypeId))
                bodyItem.LaborTypeId = code;
            else
                code = bodyItem.LaborTypeId;

            var entity = await this.Find(machineGroupId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentMachineGroups?.LaborTypeList.AddNew();
                entity.SetDefaults();
            }

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string machineGroupId, string id)
        {
            var entity = await this.Find(machineGroupId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Labor Type ID {0} could not be found for Machine Group ID '{1}.'", id, machineGroupId));

            this.CurrentMachineGroups.LaborTypeList.Remove(entity);
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
            Action<MachineLabor> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MachineLabor);
        }
        #endregion Event Handlers

        #region Properties
        protected MachineGroupsProvider Provider { get; } = new MachineGroupsProvider();

        protected MachineGroups CurrentMachineGroups { get; set; }

        protected SortedDictionary<string, Action<MachineLabor>> PropertyDictionary { get; } = new SortedDictionary<string, Action<MachineLabor>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "7cc501bf-1359-42aa-be46-36a2e6f5d6e5";
        #endregion Fields
    }
}
