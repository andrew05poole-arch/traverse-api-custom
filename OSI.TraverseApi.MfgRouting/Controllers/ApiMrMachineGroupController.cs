#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMrMachineGroupController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "machinegroup/{id?}", typeof(MachineGroups))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "machinegroup/{id?}", typeof(MachineGroups))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "machinegroup/{id?}", typeof(MachineGroups))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "machinegroup/{id}", typeof(MachineGroups))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<MachineGroups>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.MachineGroupId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<MachineGroups>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<MachineGroupsBase.Columns>();
                    builder.AppendEquals(MachineGroupsBase.Columns.MachineGroupId, id);
                    var list = new MachineGroupsProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<MachineGroups> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.MachineGroupId, id, false));
        }

        protected virtual async Task<List<MachineGroups>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Machine Group ID is provided along with more than one record.");

            var entityList = new List<MachineGroups>();
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

        protected virtual async Task<MachineGroups> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.MachineGroupId) || string.IsNullOrWhiteSpace(bodyItem.MachineGroupId))
                bodyItem.MachineGroupId = code;
            else
                code = bodyItem.MachineGroupId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new MachineGroups(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Work Center ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }
        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Machine Group ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "LaborTypeList")
            {
                if (((MachineGroups)args.ParentObject).IsNew)
                    return this.CreateDetail((MachineGroups)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Detail Update Methods
        protected virtual MachineLabor CreateDetail(MachineGroups parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborTypeId))
                throw new InvalidValueException("Labor Type ID is required.");
            this.FilterEntityList(parent.LaborTypeList, ApiMrMachineGroupLaborTypeController.FunctionID);
            MachineLabor entity = parent.LaborTypeList?.Find(MachineLaborBase.Columns.LaborTypeId, bodyItem.LaborTypeId);
            if (entity != null)
                throw new InvalidValueException(string.Format("Labor Type ID '{0}' with Machine Group ID '{1}' already exists.",bodyItem.LaborTypeId, bodyItem.MachineGroupId));
            
            entity = parent.LaborTypeList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as MachineLabor;
            entity.PropertyChanged -= Detail_PropertyChanged;
        }
        #endregion Detail Update Methods
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
            var entity = sender as MachineGroups;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<MachineGroups> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<MachineLabor> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as MachineLabor);
        }
        #endregion Event Handlers

        #region Properties
        private MachineGroupsProvider Provider { get; } = new MachineGroupsProvider();
        protected SortedDictionary<string, Action<MachineGroups>> PropertyDictionary { get; } = new SortedDictionary<string, Action<MachineGroups>>();

        protected SortedDictionary<string, Action<MachineLabor>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<MachineLabor>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "946226F5-A399-4A56-A9FC-FF5BE6C4803E";
        #endregion Fields
    }
}
