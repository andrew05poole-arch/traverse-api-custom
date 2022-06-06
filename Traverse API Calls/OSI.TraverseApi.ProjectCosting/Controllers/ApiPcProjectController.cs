#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TraverseApi;
using Task = System.Threading.Tasks.Task;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.ProjectCosting.Controllers
{
    public class ApiPcProjectController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "project/{id:int?}", typeof(Project))]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "project/{id:int?}", typeof(Project))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "project", typeof(Project))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "project/{id:int}", typeof(Project))]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            //Project Property Changes
            PropertyDictionary.Add(ProjectBase.Columns.CustId.ToString(), (entity) => entity.SetCustomerDefaults());

            //Extension Project Property Changes
            ExtensionPropertyDictionary.Add(ProjectExtension.Columns.FixedFee.ToString(), (entity) => entity.CalculateFixedFeeAmountToBill());
            ExtensionPropertyDictionary.Add(ProjectExtension.Columns.Rep1Id.ToString(), (entity) => entity.SetSalesRepDefaults(1));
            ExtensionPropertyDictionary.Add(ProjectExtension.Columns.Rep2Id.ToString(), (entity) => entity.SetSalesRepDefaults(2));

            //Project Task Property Changes
            TaskPropertyDictionary.Add(ProjectDetailBase.Columns.TaskId.ToString(), (entity) => entity.SetDefaults());
            TaskPropertyDictionary.Add(ProjectDetailBase.Columns.FixedFee.ToString(), (entity) => { if (entity.IsTask) entity.CalculateFixedFeeAmountToBill(); entity.Parent.CalculateFixedFeeTotal(); });
            TaskPropertyDictionary.Add(ProjectDetailBase.Columns.Rep1Id.ToString(), (entity) => entity.SetSalesRepDefaults(1));
            TaskPropertyDictionary.Add(ProjectDetailBase.Columns.Rep2Id.ToString(), (entity) => entity.SetSalesRepDefaults(2));

            //Site Info Property Changes
            SiteInfoPropertyDictionary.Add(ProjectDetailSiteInfoBase.Columns.SiteId.ToString(), (entity) => entity.SetShipToDefaults());
        }

        protected virtual async Task<EntityList<Project>> Load(int? id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i =>id == i.Id)))
            {
                if (id == null)
                    await Provider.Load<Project>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<ProjectBase.Columns>();
                    builder.AppendEquals(ProjectBase.Columns.Id, id.ToString());
                    var list = new ProjectProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Project> Find(int id)
        {
            var list = await Load(id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<Project>> ProcessEditRequest(bool isCreate, dynamic body, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new List<Project>();
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

        protected virtual async Task<Project> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = new Project(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "ProjectExtension")
            {
                ProjectExtension extension = ((Project)args.ParentObject).ProjectExtension;
                extension.PropertyChanged += Extension_PropertyChanged;

                return extension;
            }
            else if (args.PropertyName == "ProjectTaskList")
            {
                if (((Project)args.ParentObject).IsNew)
                    return this.CreateProjectTask((Project)args.ParentObject, args.ItemModel);
            }
            else if (args.PropertyName == "ProjectDetailSiteInfo")
            {
                if (((ProjectExtension)args.ParentObject).IsNew)
                        return this.CreateSiteInfo((ProjectExtension)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateSiteInfo((ProjectExtension)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        protected virtual ProjectTask CreateProjectTask(Project parent, dynamic bodyItem)
        {
            ProjectTask entity = parent.ProjectTaskList.AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ProjectTaskUpdateComplete;
            entity.PropertyChanged += ProjectTask_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void ProjectTaskUpdateComplete(object entityObject)
        {
            var entity = entityObject as ProjectTask;
            entity.PropertyChanged -= ProjectTask_PropertyChanged;
        }

        protected virtual ProjectDetailSiteInfo CreateSiteInfo (ProjectExtension parent, dynamic bodyItem)
        {
            parent?.CreateProjectDetailSiteInfo();
            ProjectDetailSiteInfo entity = parent?.ProjectDetailSiteInfo;

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SiteInfoUpdateComplete;
            entity.PropertyChanged += SiteInfo_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual ProjectDetailSiteInfo UpdateSiteInfo(ProjectExtension parent, dynamic bodyItem)
        {
            ProjectDetailSiteInfo entity = parent.ProjectDetailSiteInfo;
            if (entity == null)
                throw new InvalidValueException("Site Info could not be found.");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = SiteInfoUpdateComplete;
            entity.PropertyChanged += SiteInfo_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void SiteInfoUpdateComplete(object entityObject)
        {
            var entity = entityObject as ProjectDetailSiteInfo;
            entity.PropertyChanged -= SiteInfo_PropertyChanged;
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
            Action<Project> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Project);
        }

        private void Extension_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ProjectExtension> action = null;
            if (ExtensionPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ProjectExtension);
        }

        private void ProjectTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ProjectTask> action = null;
            if (TaskPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ProjectTask);
        }

        private void SiteInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ProjectDetailSiteInfo> action = null;
            if (SiteInfoPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ProjectDetailSiteInfo);
        }
        #endregion Event Handlers

        #region Properties
        private ProjectProvider Provider { get; } = new ProjectProvider();
        protected SortedDictionary<string, Action<Project>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Project>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        protected SortedDictionary<string, Action<ProjectExtension>> ExtensionPropertyDictionary { get; } = new SortedDictionary<string, Action<ProjectExtension>>();
        protected SortedDictionary<string, Action<ProjectTask>> TaskPropertyDictionary { get; } = new SortedDictionary<string, Action<ProjectTask>>();
        protected SortedDictionary<string, Action<ProjectDetailSiteInfo>> SiteInfoPropertyDictionary { get; } = new SortedDictionary<string, Action<ProjectDetailSiteInfo>>();
        #endregion Properties 

        #region Fields
        public const string FunctionID = "eab33a8d-24e4-4334-9741-1a97c917fc80";
        #endregion Fields
    }
}
