#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TraverseApi;
using Task = System.Threading.Tasks.Task;
#endregion Using Directives

namespace OSI.TraverseApi.ProjectCosting.Controllers
{
    public class ApiPcProjectTaskController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "project/{projectid:int}/task/{id:int?}", typeof(ProjectTask))]
        public async Task<IHttpActionResult> Get(int projectId = 0, int? id = null)
        {
            return Ok(await this.Load(projectId, id));
        }

        [ApiRoute(FunctionID, 2f, "project/{projectid:int}/task/{id:int?}", typeof(ProjectTask))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int projectId = 0,  int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body,projectId, id));         
        }

        [ApiRoute(FunctionID, 2f, "project/{projectid:int}/task/", typeof(ProjectTask))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int projectId = 0)
        {
            return Ok(await ProcessEditRequest(true, body, projectId)); 
        }

        [ApiRoute(FunctionID, 2f, "project/{projectid:int}/task/{id:int}", typeof(ProjectTask))]
        public async Task Delete(int projectId, int id)
        {
            await this.MarkToDelete(projectId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() {
            //Project Task Property Changes
            PropertyDictionary.Add(ProjectDetailBase.Columns.TaskId.ToString(), (entity) => entity.SetDefaults());
            PropertyDictionary.Add(ProjectDetailBase.Columns.FixedFee.ToString(),(entity) => { if (entity.IsTask) entity.CalculateFixedFeeAmountToBill(); entity.Parent.CalculateFixedFeeTotal();});
            PropertyDictionary.Add(ProjectDetailBase.Columns.Rep1Id.ToString(), (entity) => entity.SetSalesRepDefaults(1));
            PropertyDictionary.Add(ProjectDetailBase.Columns.Rep2Id.ToString(), (entity) => entity.SetSalesRepDefaults(2));
        }

        protected virtual async Task<EntityList<ProjectTask>> Load(int projectId, int? id)
        {
            var list = CurrentProject?.ProjectTaskList;

            if (CurrentProject == null || CurrentProject.Id != projectId)
            {
                var builder = new SqlFilterBuilder<ProjectBase.Columns>();
                builder.AppendEquals(ProjectBase.Columns.Id, projectId.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiPcProjectController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Project ID '{0}' could not be found.", projectId));

                CurrentProject = Provider.Items[0];

                list = CurrentProject.ProjectTaskList;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list?.FindAll(x => x.Id == id);
            return list;
        }

        protected virtual async Task<ProjectTask> Find(int projectId, int id)
        {
            var list = await Load(projectId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<ProjectTask>> ProcessEditRequest(bool isCreate, dynamic body, int projectId, int? id = null)
        {           
           object[] list;

           if (body is object[])
               list = body;
           else
               list = new object[1] { body };

           if (list.Length > 1 && id.HasValue)
               throw new InvalidValueException("Call is ambiguous. Project Task ID is provided along with more than one record.");

            var entityList = new List<ProjectTask>();
            foreach (dynamic item in list)
           {
               var entity = await this.ProcessBodyItem(isCreate, item, projectId, id);

               if (!entityList.Contains(entity))
                    entityList.Add(entity);
           }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

           return entityList; 
        }

        protected virtual async Task<ProjectTask> ProcessBodyItem(bool isCreate, dynamic bodyItem, int projectId, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = (int)bodyItem.Id;

            var entity = await this.Find(projectId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentProject.ProjectTaskList.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Project Task ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int projectId, int id)
        {
            var entity = await this.Find(projectId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Project Task ID '{0}' in Project ID '{1}' does not exist.", id, projectId));

            CurrentProject.ProjectTaskList.Remove(entity);
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
            Action<ProjectTask> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ProjectTask);
        }
        #endregion Event Handlers

        #region Properties
        protected Project CurrentProject { get; set; }
        private ProjectProvider Provider { get; } = new ProjectProvider();
        protected SortedDictionary<string, Action<ProjectTask>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ProjectTask>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "326c00b9-2ce2-4347-937c-27722a7a0082";
        #endregion Fields
    }
}
