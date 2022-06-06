#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CRM;
using TRAVERSE.Core;
using TraverseApi;
using T = System.Threading.Tasks;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.CRM.Controllers
{
    public class ApiCmCampaignController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "campaign/{id:long?}", typeof(Campaign))]
        public async Task<IHttpActionResult> Get(long? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "campaign/{id:long?}", typeof(Campaign))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "campaign", typeof(Campaign))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "campaign/{id:long}", typeof(Campaign))]
        public async T.Task Delete(long id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<Campaign>> Load(long? id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.Id == id)))
            {
                if (id == null)
                    await Provider.Load<Campaign>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<CampaignBase.Columns>();
                    builder.AppendEquals(CampaignBase.Columns.Id, id.ToString());
                    var list = new CampaignProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<Campaign> Find(long id)
        {
            var list = await Load(id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<Campaign>> ProcessEditRequest(bool isCreate, dynamic body, long? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Campaign ID is provided along with more than one record.");

            var entityList = new List<Campaign>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Campaign> ProcessBodyItem(bool isCreate, dynamic bodyItem, long? id)
        {
            long code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt64(bodyItem.Id);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = new Campaign(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Campaign ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(long id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Campaign ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (StringHelper.AreEqual(args.PropertyName, "CampaignDetailList", false))
            {
                if (((Campaign)args.ParentObject).IsNew)
                    return this.CreateCampaignDetail((Campaign)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateCampaignDetail((Campaign)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Detail
        protected virtual CampaignDetail UpdateCampaignDetail(Campaign parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Detail ID is required.");

            CampaignDetail entity = parent.CampaignDetailList?.Find(x => x.Id == Convert.ToInt64(bodyItem.Id));
            if (entity == null)
                throw new InvalidValueException(string.Format("Detail ID '{0}' for Campaign ID '{1}' could not be found.",
                    bodyItem.Id, parent.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += CampaignDetail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual CampaignDetail CreateCampaignDetail(Campaign parent, dynamic bodyItem)
        {
            CampaignDetail entity = parent.CampaignDetailList.AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += CampaignDetail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as CampaignDetail;
            entity.PropertyChanged -= CampaignDetail_PropertyChanged;
        }
        #endregion Detail
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
            Action<Campaign> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Campaign);
        }

        private void CampaignDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<CampaignDetail> action = null;
            if (CampaignDetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as CampaignDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected CampaignProvider Provider { get; } = new CampaignProvider();

        protected SortedDictionary<string, Action<Campaign>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Campaign>>();

        protected SortedDictionary<string, Action<CampaignDetail>> CampaignDetailPropertyDictionary { get; } = new SortedDictionary<string, Action<CampaignDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "3f46f03f-d22a-4adb-8624-261ba5e06273";
        #endregion Fields
    }
}
