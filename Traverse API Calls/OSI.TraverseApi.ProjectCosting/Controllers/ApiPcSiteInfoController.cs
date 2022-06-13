#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.ProjectCosting.Controllers
{
    public class ApiPcSiteInfoController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "project/{projectid:int}/siteinfo", typeof(ProjectDetailSiteInfo))]
        public async Task<IHttpActionResult> Get(int projectId = 0)
        {
            return Ok(await this.Load(projectId));
        }

        [ApiRoute(FunctionID, 2f, "project/{projectid:int}/siteinfo", typeof(ProjectDetailSiteInfo))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int projectId = 0)
        {
            return Ok(await ProcessEditRequest(false, body, projectId));
        }

        [ApiRoute(FunctionID, 2f, "project/{projectid:int}/siteinfo", typeof(ProjectDetailSiteInfo))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int projectId = 0)
        {
            return Ok(await ProcessEditRequest(true, body, projectId));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(ProjectDetailSiteInfoBase.Columns.SiteId.ToString(),(entity) => entity.SetShipToDefaults());
        }

        protected virtual async Task<ProjectDetailSiteInfo> Load(int id)
        {
            if (Provider.Items.Count <= 0 || (!Provider.Items.Exists(i => id == i.Id)))
            {               
                    var builder = new SqlFilterBuilder<ProjectBase.Columns>();
                    builder.AppendEquals(ProjectBase.Columns.Id, id.ToString());
                    var list = new ProjectProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);

                await this.FilterEntityListAsync(Provider.Items, ApiPcProjectController.FunctionID);
            }
            CurrentProject = Provider.Items.Find(x => x.Id == id);
            return CurrentProject?.ProjectExtension?.ProjectDetailSiteInfo;
        }

        protected virtual async Task<List<ProjectDetailSiteInfo>> ProcessEditRequest(bool isCreate, dynamic body, int projectId)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 )
                throw new InvalidValueException("Call is ambiguous. Site Info is provided along with more than one record.");

            var entityList = new List<ProjectDetailSiteInfo>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, projectId);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ProjectDetailSiteInfo> ProcessBodyItem(bool isCreate, dynamic bodyItem, int projectId)
        {
            var entity = await this.Load(projectId);

            if (isCreate)
            {
                if (CurrentProject?.CustId != null)
                {
                    if (entity != null)
                        return entity;

                    CurrentProject?.ProjectExtension?.CreateProjectDetailSiteInfo();
                    entity = CurrentProject?.ProjectExtension?.ProjectDetailSiteInfo;
                } 
                else
                    throw new InvalidValueException("Customer ID is required.");          
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Site Info for Project ID '{0}' could not be found.", projectId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
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
            Action<ProjectDetailSiteInfo> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ProjectDetailSiteInfo);
        }
        #endregion Event Handlers

        #region Properties
        private Project CurrentProject { get; set; }
        private ProjectProvider Provider { get; } = new ProjectProvider();
        protected SortedDictionary<string, Action<ProjectDetailSiteInfo>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ProjectDetailSiteInfo>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "4db715d9-0b0a-416b-a599-a89d06bf342f";
        #endregion Fields
    }
}
