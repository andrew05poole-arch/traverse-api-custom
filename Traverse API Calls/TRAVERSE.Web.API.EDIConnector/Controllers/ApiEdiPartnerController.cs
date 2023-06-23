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
    public class ApiEdiPartnerController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "partner/{partnerId}", typeof(Partner))]
        public async Task<IHttpActionResult> Get(string partnerId)
        {
            return Ok(await this.Load(partnerId));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId?}", typeof(Partner))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string partnerId = null)
        {
            return Ok(await ProcessEditRequest(false, body, partnerId));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId?}", typeof(Partner))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string partnerId = null)
        {
            return Ok(await ProcessEditRequest(true, body, partnerId));
        }

        [ApiRoute(FunctionID, 2f, "partner/{partnerId}", typeof(Partner))]
        public async Task Delete(string partnerId)
        {
            await this.MarkToDelete(partnerId);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
        }

        protected virtual async Task<EntityList<Partner>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.PartnerId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Partner>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<Partner.Columns>();
                    builder.AppendEquals(Partner.Columns.PartnerId, id);
                    var list = new PartnerProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Partner> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.PartnerId, id, false));
        }

        protected virtual async Task<List<Partner>> ProcessEditRequest(bool isCreate, dynamic body, string partnerId)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            //if (list.Length > 1 && !string.IsNullOrEmpty(id))
            //    throw new InvalidValueException("Call is ambiguous. Partner ID is provided along with more than one record.");

            var entityList = new List<Partner>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, partnerId);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Partner> ProcessBodyItem(bool isCreate, dynamic bodyItem, string partnerId)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PartnerId) || string.IsNullOrWhiteSpace(bodyItem.PartnerId))
                bodyItem.PartnerId = partnerId;
            else
                partnerId = bodyItem.PartnerId;

            var entity = await this.Find(partnerId);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Partner(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Partner '{0}' could not be found.", partnerId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string partnerId)
        {
            var entity = await this.Find(partnerId);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Partner '{0}' could not be found.", partnerId));

            this.Provider.Items.Remove(entity);
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
            Action<Partner> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Partner);
        }
        #endregion Event Handlers

        #region Properties
        protected PartnerProvider Provider { get; } = new PartnerProvider();

        protected SortedDictionary<string, Action<Partner>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Partner>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "EF0BDDEC-4A6E-48AD-9889-FC9E6D4CEA02";
        #endregion Fields
    }
}
