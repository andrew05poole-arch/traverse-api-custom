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
    public class ApiEdiPartnerDocumentController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnerdoc/{docId}", typeof(PartnerDoc))]
        public async Task<IHttpActionResult> Get(string partnerId, long docId)
        {
            return Ok(await this.Load(partnerId, docId, false));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnerdoc/{docId?}", typeof(PartnerDoc))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string partnerId, long? docId = null)
        {
            return Ok(await ProcessEditRequest(false, body, partnerId, docId));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnerdoc/{docId?}", typeof(PartnerDoc))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string partnerId, long? docId = null)
        {
            return Ok(await ProcessEditRequest(true, body, partnerId, docId));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId}/partnerdoc/{docId}", typeof(PartnerDoc))]
        public async Task Delete(string partnerId, long docId)
        {
            await this.MarkToDelete(partnerId, docId);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(PartnerDocBase.Columns.MailIdKeyType.ToString(), MailIdKeyTypePropertyChanged);
        }

        protected virtual async Task<EntityList<PartnerDoc>> Load(string partnerId, long? docId, bool isCreate)
        {
            var list = CurrentPartner?.PartnerDocDetailList as EntityList<PartnerDoc>;

            if (CurrentPartner == null || !this.Provider.Items.Exists(i => i.PartnerId == partnerId))
            {
                var builder = new SqlFilterBuilder<PartnerBase.Columns>();
                builder.AppendEquals(PartnerBase.Columns.PartnerId, partnerId.ToString());
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiEdiPartnerController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Partner '{0}' could not be found.", partnerId));

                CurrentPartner = Provider.Items[0];

                list = CurrentPartner.PartnerDocDetailList as EntityList<PartnerDoc>;
                await this.FilterEntityListAsync(list);
            }

            if (docId.HasValue && !isCreate)
            {
                list = list.FindAll(PartnerDocBase.Columns.DocId, docId.Value);
                if (list.Count <= 0)
                {
                    throw new InvalidValueException(string.Format("Partner Document '{0}' could not be found on Partner '{1}'.", docId.Value, partnerId));
                }
                else
                    return list;
            }

            return list;
        }

        protected virtual async Task<PartnerDoc> Find(string partnerId, long? docId, bool isCreate)
        {
            var list = await Load(partnerId, docId, isCreate);
            return list.Find(x => x.DocId == docId);
        }

        protected virtual async Task<List<PartnerDoc>> ProcessEditRequest(bool isCreate, dynamic body, string partnerId, long? docId)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && docId.HasValue)
                throw new InvalidValueException("Call is ambiguous. Partner Document is provided along with more than one record.");

            var entityList = new List<PartnerDoc>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, partnerId, docId);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PartnerDoc> ProcessBodyItem(bool isCreate, dynamic bodyItem, string partnerId, long? docId)
        {
            if (docId.HasValue && (ApiUserSkipped.IsApiUserSkipped(bodyItem.DocId) || bodyItem.DocId == null))
                bodyItem.DocId = docId;
            else
                docId = Convert.ToInt64(bodyItem.DocId);

            var entity = await this.Find(partnerId, docId, isCreate);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = this.CurrentPartner.PartnerDocDetailList.AddNew();
                entity.EdiMailKeyType = MailKeyType.NoOverride;
                entity.SetDocumentDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Partner Document '{0}' could not be found on Partner '{1}'.", docId, partnerId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string partnerId, long docId)
        {
            var entity = await this.Find(partnerId, docId, false);

            if (entity == null)
                throw new InvalidValueException(string.Format("Partner Document '{0}' could not be found on Partner '{1}'", docId, partnerId));

            CurrentPartner.PartnerDocDetailList.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void MailIdKeyTypePropertyChanged(PartnerDoc entity)
        {
            if (entity.EdiMailKeyType == null)
            {
                entity.EdiMailKeyType = MailKeyType.NoOverride;
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
            Action<PartnerDoc> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PartnerDoc);
        }
        #endregion Event Handlers

        #region Properties
        protected PartnerProvider Provider { get; } = new PartnerProvider();

        protected Partner CurrentPartner { get; set; }

        protected SortedDictionary<string, Action<PartnerDoc>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PartnerDoc>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "413FB54E-B78A-449B-B20E-5A04C5402B75";
        #endregion Fields
    }
}
