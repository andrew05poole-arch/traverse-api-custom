using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.EDI;
using TRAVERSE.Core;

namespace TRAVERSE.Web.API.EDIConnector.Controllers
{
    public class ApiEdiPartnerCrossReferenceController : ApiControllerBase
    {
        #region Web Methods

        [ApiRoute(FunctionID, 2f, "partner/{partnerId:long?}/doc/{docId:long?}", typeof(PartnerDocOverride))]
        public async Task<IHttpActionResult> Get(long? partnerId = null, long? docId = null)
        {
            return Ok(await this.Load(false, partnerId, docId));
        }
        [ApiRoute(FunctionID, 2f, "partner/{partnerId:long?}/doc/{docId:long?}", typeof(PartnerDocOverride))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long? partnerId = null, long? docId = null)
        {
               return Ok(await ProcessEditRequest(false, body, partnerId, docId));
        }
        
        [ApiRoute(FunctionID, 2f, "partner/{partnerId:long?}/doc/{docId:long?}", typeof(PartnerDocOverride))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, long? partnerId = null, long? docId = null)
        {
            return Ok(await ProcessEditRequest(true, body, partnerId, docId));
        }

        [ApiRoute(FunctionID, 2f, "partnercrossref/{id:long}", typeof(PartnerDocOverride))]
        public async Task Delete(long Id)
        {
            await this.MarkToDelete(Id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }
       
        protected virtual async Task<EntityList<PartnerDocOverride>> Load(bool isCreate, long? partnerId, long? docId)
        {
            if (Provider.Items.Count <= 0
                || !Provider.Items.Exists(x => x.PartnerId == partnerId)
                || !Provider.Items.Exists(x => x.DocId== docId))
            {
                var builder = new SqlFilterBuilder<PartnerDocOverride.Columns>();

                if (partnerId!=null)
                    builder.AppendEquals(PartnerDocOverrideBase.Columns.PartnerId, Convert.ToString(partnerId));
                if (docId != null)
                        builder.AppendEquals(PartnerDocOverrideBase.Columns.DocId, Convert.ToString(docId));

                var list = new PartnerDocOverrideProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                Provider.Items.AddRange(list);

                if (Provider.Items.Count <= 0 && !isCreate)
                {
                    if (partnerId==null && docId != null)
                        throw new NothingToProcessException(string.Format("Partner ID '{0}' could not be found.", partnerId));
                    if (partnerId!=null && docId == null)
                        throw new NothingToProcessException(string.Format("Doc ID '{0}' could not be found.", docId));
                    else
                        throw new NothingToProcessException(string.Format("Partner ID '{0}' with Doc ID '{1}' could not be found.", partnerId, docId));
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }
        protected virtual async Task<List<PartnerDocOverride>> ProcessEditRequest(bool isCreate, dynamic body, long? partnerId = null, long? docId = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && (partnerId!=null && docId != null))
                throw new InvalidValueException("Call is ambiguous. Parnter ID and Doc ID are provided along with more than one record.");

            var entityList = new List<PartnerDocOverride>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, partnerId, docId);
                if (isCreate) this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<PartnerDocOverride> ProcessBodyItem(bool isCreate, dynamic bodyItem, long? partnerId, long? docId)
        {
            long partner = partnerId.GetValueOrDefault();
            long doc = docId.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PartnerId) || bodyItem.PartnerId==null)    //string.IsNullOrWhiteSpace(bodyItem.PartnerId)
                bodyItem.PartnerId = partner;
            else
                partner = bodyItem.PartnerId;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.DocId) || bodyItem.DocId == null)
                bodyItem.DocId = doc;
            else
                doc = bodyItem.DocId;

            var entity = await this.Find(isCreate, partner, doc);

            if (isCreate)
            {
                //if (entity != null)
                //    return entity;

                entity = new PartnerDocOverride(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Partner ID '{0}' with Doc ID '{1}' could not be found.", partner, doc));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(long id)
        {
            var entity = await this.Find(id);

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }
        protected virtual async Task<PartnerDocOverride> Find(bool isCreate, long partnerId, long docId)
        {
            var list = await Load(isCreate, partnerId, docId);
            return list.Find(x => x.PartnerId == partnerId && x.DocId == docId);
        }
        protected virtual async Task<PartnerDocOverride> Find(long id)
        {
            var list = await Load(id);
            return list.Find(x => x.Id == id);
        }
        protected virtual async Task<EntityList<PartnerDocOverride>> Load(long id)
        {
            if (CurrentItem != null && CurrentItem.Id == Convert.ToInt64(id))
                return null;

            SqlFilterBuilder<PartnerDocOverrideBase.Columns> builder = new SqlFilterBuilder<PartnerDocOverrideBase.Columns>();
            builder.AppendEquals(PartnerDocOverrideBase.Columns.Id, id.ToString());

            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            await this.FilterEntityListAsync(Provider.Items, ApiEdiPartnerCrossReferenceController.FunctionID);
            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("PartnerCrossRef '{0}' could not be found.", id));

            return Provider.Items;
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
            Action<PartnerDocOverride> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PartnerDocOverride);
        }
        #endregion Event Handlers

        #region Properties
        protected PartnerDocOverrideProvider Provider { get; } = new PartnerDocOverrideProvider();

        protected PartnerDocOverride CurrentItem { get; set; }

        protected SortedDictionary<string, Action<PartnerDocOverride>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PartnerDocOverride>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "010b67cd-0764-4b47-b17b-5a025003e0e0";

        #endregion Properties
    }
}
