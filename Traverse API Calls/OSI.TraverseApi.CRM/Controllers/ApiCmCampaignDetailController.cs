#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CRM;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using T = System.Threading.Tasks;
#endregion Using Directives

namespace TRAVERSE.Web.API.CRM.Controllers
{
    public class ApiCmCampaignDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "campaign/{campaignid:long}/detail/{id:long?}", typeof(CampaignDetail))]
        public async Task<IHttpActionResult> Get(long campaignId, long? id = null)
        {
            return Ok(await this.Load(campaignId, id));
        }

        [ApiRoute(FunctionID, 2f, "campaign/{campaignid:long}/detail/{id:long?}", typeof(CampaignDetail))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long campaignId, long? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, campaignId, id));
        }

        [ApiRoute(FunctionID, 2f, "campaign/{campaignid:long}/detail", typeof(CampaignDetail))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, long campaignId)
        {
            return Ok(await ProcessEditRequest(true, body, campaignId, null));
        }

        [ApiRoute(FunctionID, 2f, "campaign/{campaignid:long}/detail/{id:long}", typeof(CampaignDetail))]
        public async T.Task Delete(long campaignId, long id)
        {
            await this.MarkToDelete(campaignId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<CampaignDetail>> Load(long campaignId, long? id)
        {
            var list = this.CurrentCampaign?.CampaignDetailList;

            if (this.CurrentCampaign == null || this.CurrentCampaign.Id != campaignId)
            {
                var builder = new SqlFilterBuilder<CampaignBase.Columns>();
                builder.AppendEquals(CampaignBase.Columns.Id, campaignId.ToString());
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiCmCampaignController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Campaign ID '{0}' could not be found.", campaignId));

                this.CurrentCampaign = Provider.Items[0];

                await FilterEntityListAsync(this.CurrentCampaign.CampaignDetailList, FunctionID);

                list = this.CurrentCampaign.CampaignDetailList;
            }
            
            if (id.HasValue)
                return list.FindAll(CampaignDetailBase.Columns.Id, id.Value);

            return list;
        }

        protected virtual async Task<CampaignDetail> Find(long campaignId, long id)
        {
            var list = await Load(campaignId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<CampaignDetail>> ProcessEditRequest(bool isCreate, dynamic body, long campaignId, long? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Detail ID is provided along with more than one record.");

            var entityList = new List<CampaignDetail>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, campaignId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<CampaignDetail> ProcessBodyItem(bool isCreate, dynamic bodyItem, long campaignId, long? id)
        {
            long code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt64(bodyItem.Id);

            var entity = await this.Find(campaignId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = this.CurrentCampaign?.CampaignDetailList?.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Detail ID '{0}' could not be found on Campaign ID {1}.", code, campaignId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(long campaignId, long id)
        {
            var entity = await this.Find(campaignId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Detail ID '{0}' could not be found on Campaign ID {1}.", id, campaignId));

            this.CurrentCampaign?.CampaignDetailList?.Remove(entity);
            this.Provider?.Update(this.CompId);
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
            Action<CampaignDetail> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as CampaignDetail);
        }
        #endregion Event Handlers

        #region Properties
        protected CampaignProvider Provider { get; } = new CampaignProvider();

        protected Campaign CurrentCampaign { get; set; }

        protected SortedDictionary<string, Action<CampaignDetail>> PropertyDictionary { get; } = new SortedDictionary<string, Action<CampaignDetail>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "6b645f37-7e66-4a6e-9f5c-5f0aed57304f";
        #endregion Fields
    }
}
