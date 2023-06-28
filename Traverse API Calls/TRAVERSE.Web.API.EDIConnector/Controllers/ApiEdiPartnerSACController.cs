using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Core;
using TRAVERSE.Business.API;
using TRAVERSE.Business.EDI;
using System.ComponentModel;

namespace TRAVERSE.Web.API.EDIConnector.Controllers
{
    public class ApiEdiPartnerSACController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnersac/{sacCode}", typeof(PartnerSAC))]
        public async Task<IHttpActionResult> Get(string partnerId, string sacCode)
        {
            return Ok(await this.Load(partnerId, sacCode, false));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnersac/{sacCode?}", typeof(PartnerSAC))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string partnerId, string sacCode = null)
        {
            return Ok(await ProcessEditRequest(false, body, partnerId, sacCode));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnersac/{sacCode?}", typeof(PartnerSAC))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string partnerId, string sacCode = null)
        {
            return Ok(await ProcessEditRequest(true, body, partnerId, sacCode));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnersac/{sacCode}", typeof(PartnerSAC))]
        public async Task Delete(string partnerId, string sacCode)
        {
            await this.MarkToDelete(partnerId, sacCode);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(PartnerDocBase.Columns.MailIdKeyType.ToString(), SACAdjTypePropertyChanged);
            PropertyDictionary.Add(PartnerDocBase.Columns.MailIdKeyType.ToString(), SACIndicatorPropertyChanged);
        }

        protected virtual async Task<EntityList<PartnerSAC>> Load(string partnerId, string sacCode, bool isCreate)
        {
            var list = CurrentPartner?.PartnerSacDetailList as EntityList<PartnerSAC>;

            if (CurrentPartner == null || !this.Provider.Items.Exists(i => i.PartnerId == partnerId))
            {
                var builder = new SqlFilterBuilder<PartnerBase.Columns>();
                builder.AppendEquals(PartnerBase.Columns.PartnerId, partnerId.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiEdiPartnerController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Partner '{0}' could not be found.", partnerId));

                CurrentPartner = Provider.Items[0];

                list = CurrentPartner.PartnerSacDetailList as EntityList<PartnerSAC>;
                await this.FilterEntityListAsync(list);
            }

            if (!string.IsNullOrEmpty(sacCode) && !isCreate)
            {
                list = list.FindAll(PartnerSACBase.Columns.SACCode, sacCode);
                if (list.Count <= 0)
                {
                    throw new InvalidValueException(string.Format("Partner SAC '{0}' could not be found on Partner '{1}'.", sacCode, partnerId));
                }
                else
                    return list;
            }

            return list;
        }

        protected virtual async Task<PartnerSAC> Find(string partnerId, string sacCode, bool isCreate)
        {
            var list = await Load(partnerId, sacCode, isCreate);
            return list.Find(x => StringHelper.AreEqual(x.SACCode, sacCode));
        }

        protected virtual async Task<List<PartnerSAC>> ProcessEditRequest(bool isCreate, dynamic body, string partnerId, string sacCode)
        {
            sacCode = (StringHelper.AreEqual(sacCode, "undefined") || StringHelper.AreEqual(sacCode, "{sacCode}")) ? null : sacCode;

            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(sacCode))
                throw new InvalidValueException("Call is ambiguous. Partner SAC is provided along with more than one record.");

            var entityList = new List<PartnerSAC>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, partnerId, sacCode);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PartnerSAC> ProcessBodyItem(bool isCreate, dynamic bodyItem, string partnerId, string sacCode)
        {
            sacCode = (StringHelper.AreEqual(sacCode, "undefined") || StringHelper.AreEqual(sacCode, "{sacCode}")) ? null : sacCode;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SACCode) || bodyItem.SACCode == null || StringHelper.AreEqual(Convert.ToString(bodyItem.SACCode), "undefined"))
                bodyItem.SACCode = sacCode;
            else
                sacCode = Convert.ToString(bodyItem.SACCode);

            var entity = await this.Find(partnerId, sacCode, isCreate);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentPartner.PartnerSacDetailList.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Partner SAC '{0}' could not be found on Partner '{1}'.", sacCode, partnerId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string partnerId, string sacCode)
        {
            var entity = await this.Find(partnerId, sacCode, false);

            if (entity == null)
                throw new InvalidValueException(string.Format("Partner SAC '{0}' could not be found on Partner '{1}'", sacCode, partnerId));

            CurrentPartner.PartnerSacDetailList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void SACAdjTypePropertyChanged(PartnerSAC entity)
        {
            if (entity.SACAdjType == null)
            {
                entity.SACAdjustmentType = SACType.Allowance;
            }
        }

        protected virtual void SACIndicatorPropertyChanged(PartnerSAC entity)
        {
            if (entity.SACIndicator == null)
            {
                entity.SACIndicatorType = AdjustmentType.Amount;
            }
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
            Action<PartnerSAC> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PartnerSAC);
        }
        #endregion Event Handlers

        #region Properties
        protected PartnerProvider Provider { get; } = new PartnerProvider();

        protected Partner CurrentPartner { get; set; }

        protected SortedDictionary<string, Action<PartnerSAC>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PartnerSAC>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "29B09AA3-1476-4971-A71C-E818EC110C2C";
        #endregion Fields
    }
}
