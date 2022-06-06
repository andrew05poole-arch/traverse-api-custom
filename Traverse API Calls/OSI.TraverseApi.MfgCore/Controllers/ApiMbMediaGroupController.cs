#region Using Directives
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
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.MfgCore.Controllers
{
    public class ApiMbMediaGroupController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "mediagroup/{id?}", typeof(MediaGroups))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "mediagroup/{id?}", typeof(MediaGroups))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "mediagroup/{id?}", typeof(MediaGroups))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "mediagroup/{id}", typeof(MediaGroups))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<MediaGroups>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.MGId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<MediaGroups>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<MediaGroupsBase.Columns>();
                    builder.AppendEquals(MediaGroupsBase.Columns.MGId, id);
                    var list = new MediaGroupsProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<MediaGroups> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.MGId, id, false));
        }

        protected virtual async Task<List<MediaGroups>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Media Group ID is provided along with more than one record.");

            var entityList = new List<MediaGroups>();
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

        protected virtual async Task<MediaGroups> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.MGId) || string.IsNullOrWhiteSpace(bodyItem.MGId))
                bodyItem.MGId = code;
            else
                code = bodyItem.MGId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new MediaGroups(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Media Group ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Media Group ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "MediaList")
            {
                if (((MediaGroups)args.ParentObject).IsNew)
                    return this.CreateDetail((MediaGroups)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDetail((MediaGroups)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Detail Update Methods
        protected virtual Media UpdateDetail(MediaGroups parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.MId))
                throw new InvalidValueException("Media ID is required.");

            this.FilterEntityList(parent.MediaList, ApiMbMediaGroupDetailController.FunctionID);
            Media entity = parent.MediaList?.Find(MediaBase.Columns.MId, bodyItem.MId);
            if (entity == null)
                throw new InvalidValueException(string.Format("Media ID '{0}' with Media Group ID '{1}' could not be found.", bodyItem.MId, parent.MGId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual Media CreateDetail(MediaGroups parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.MId))
                throw new InvalidValueException("Media ID is required.");

            this.FilterEntityList(parent.MediaList, ApiMbMediaGroupDetailController.FunctionID);
            Media entity = parent.MediaList?.Find(MediaBase.Columns.MId, bodyItem.MId);
            if (entity != null)
                throw new InvalidValueException(string.Format("Media ID '{0}' with Media Group ID '{1}' already exists.", bodyItem.MId, parent.MGId));

            entity = parent.MediaList.AddNew();
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
            var entity = entityObject as Media;
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
            var entity = sender as MediaGroups;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<MediaGroups> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Media> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Media);
        }
        #endregion Event Handlers

        #region Properties
        protected MediaGroupsProvider Provider { get; } = new MediaGroupsProvider();

        protected SortedDictionary<string, Action<MediaGroups>> PropertyDictionary { get; } = new SortedDictionary<string, Action<MediaGroups>>();

        protected SortedDictionary<string, Action<Media>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<Media>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "711eaa61-e71c-45d1-b4ff-a570ad587375";
        #endregion Fields
    }
}
