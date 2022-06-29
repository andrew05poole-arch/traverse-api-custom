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

namespace TRAVERSE.Web.API.MfgCore.Controllers
{
    public class ApiMbMediaGroupDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "mediagroup/{mediagroupid}/detail/{id?}", typeof(Media))]
        public async Task<IHttpActionResult> Get(string mediaGroupId, string id = null)
        {
            return Ok(await Load(mediaGroupId, id));
        }

        [ApiRoute(FunctionID, 2f, "mediagroup/{mediagroupid}/detail/{id?}", typeof(Media))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string mediaGroupId, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, mediaGroupId, id));
        }

        [ApiRoute(FunctionID, 2f, "mediagroup/{mediagroupid}/detail/{id?}", typeof(Media))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string mediaGroupId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, mediaGroupId, id));
        }

        [ApiRoute(FunctionID, 2f, "mediagroup/{mediagroupid}/detail/{id}", typeof(Media))]
        public async Task Delete(string mediaGroupId, string id)
        {
            await this.MarkToDelete(mediaGroupId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<Media>> Load(string mediaGroupId, string id)
        {
            var list = this.CurrentMediaGroup?.MediaList;

            if (this.CurrentMediaGroup == null || !StringHelper.AreEqual(this.CurrentMediaGroup.MGId, mediaGroupId, false))
            {
                var builder = new SqlFilterBuilder<MediaGroupsBase.Columns>();
                builder.AppendEquals(MediaGroupsBase.Columns.MGId, mediaGroupId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Media Group ID '{0}' could not be found.", mediaGroupId));

                await this.FilterEntityListAsync(Provider.Items, ApiMbMediaGroupController.FunctionID);

                this.CurrentMediaGroup = Provider.Items[0];

                list = this.CurrentMediaGroup.MediaList;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(MediaBase.Columns.MId, id);

            return list;
        }

        protected virtual async Task<Media> Find(string mediaGroupId, string id)
        {
            var list = await Load(mediaGroupId, id);
            return list.Find(x => StringHelper.AreEqual(x.MId, id, false));
        }

        protected virtual async Task<List<Media>> ProcessEditRequest(bool isCreate, dynamic body, string mediaGroupId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Media ID is provided along with more than one record.");

            var entityList = new List<Media>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, mediaGroupId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Media> ProcessBodyItem(bool isCreate, dynamic bodyItem, string mediaGroupId, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.MId) || string.IsNullOrWhiteSpace(bodyItem.MId))
                bodyItem.MId = code;
            else
                code = bodyItem.MId;

            var entity = await this.Find(mediaGroupId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentMediaGroup?.MediaList.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Media ID {0} could not be found on Media Group ID '{1}'.", code, mediaGroupId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string mediaGroupId, string id)
        {
            var entity = await this.Find(mediaGroupId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("Media ID {0} could not be found for Media Group ID '{1}.'", id, mediaGroupId));

            this.CurrentMediaGroup.MediaList.Remove(entity);
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
            Action<Media> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Media);
        }
        #endregion Event Handlers

        #region Properties
        protected MediaGroupsProvider Provider { get; } = new MediaGroupsProvider();

        protected MediaGroups CurrentMediaGroup { get; set; }

        protected SortedDictionary<string, Action<Media>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Media>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "242e7391-0a22-480d-98dd-d63e6ccafa11";
        #endregion Fields
    }
}
