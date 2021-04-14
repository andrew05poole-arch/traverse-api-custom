#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CRM;
using TRAVERSE.Core;
using TraverseApi;
using T = System.Threading.Tasks;
#endregion Using Directives

namespace OSI.TraverseApi.CRM.Controllers
{
    public class ApiCmContactMethodController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "contact/{contactid:long}/method/{id:long?}", typeof(ContactMethod))]
        public async Task<IHttpActionResult> Get(long contactId, long? id = null)
        {
            return Ok(await this.Load(contactId, id));
        }

        [ApiRoute(FunctionID, 2f, "contact/{contactid:long}/method/{id:long?}", typeof(ContactMethod))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long contactId, long? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, contactId, id));
        }

        [ApiRoute(FunctionID, 2f, "contact/{contactid:long}/method", typeof(ContactMethod))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, long contactId)
        {
            return Ok(await ProcessEditRequest(true, body, contactId, null));
        }

        [ApiRoute(FunctionID, 2f, "contact/{contactid:long}/method/{id:long}", typeof(ContactMethod))]
        public async T.Task Delete(long contactId, long id)
        {
            await this.MarkToDelete(contactId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            PropertyDictionary.Add(ContactMethodBase.Columns.TypeId.ToString(), (entity) =>
            {
                if((CurrentContact?.ContactMethodList?.FindAll(x => x.TypeId == entity.TypeId)as EntityList<ContactMethod>)?.Count > 1)
                    throw new InvalidValueException("Contact Method already exists.");
            });
        }

        protected virtual async Task<EntityList<ContactMethod>> Load(long contactId, long? id)
        {
            var list = CurrentContact?.ContactMethodList;

            if (CurrentContact == null || CurrentContact.Id != contactId)
            {
                var builder = new SqlFilterBuilder<ContactBase.Columns>();
                builder.AppendEquals(ContactBase.Columns.Id, contactId.ToString());
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiCrmContactController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Contact ID '{0}' could not be found.", contactId));

                CurrentContact = Provider.Items[0];

                list = CurrentContact.ContactMethodList;
                await this.FilterEntityListAsync(list, FunctionID);
            }

            if (id.HasValue)
                return list.FindAll(ContactMethodBase.Columns.Id, id.Value);

            return list;
        }

        protected virtual async Task<ContactMethod> Find(long contactId, long id)
        {
            var list = await Load(contactId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<ContactMethod>> ProcessEditRequest(bool isCreate, dynamic body, long contactId, long? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Contact Method ID is provided along with more than one record.");

            var entityList = new List<ContactMethod>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, contactId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ContactMethod> ProcessBodyItem(bool isCreate, dynamic bodyItem, long contactId, long? id)
        {
            long code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt64(bodyItem.Id);

            var entity = await this.Find(contactId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                    entity = CurrentContact?.ContactMethodList?.AddNew();
                    entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Contact Method ID '{0}' could not be found on Contact ID '{1}'.",
                    code, contactId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(long contactId, long id)
        {
            var entity = await this.Find(contactId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Contact Method ID '{0}' could not be found.", id));

            this.CurrentContact.ContactMethodList.Remove(entity);
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
            Action<ContactMethod> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ContactMethod);
        }
        #endregion Event Handlers

        #region Properties
        protected ContactProvider Provider { get; } = new ContactProvider();
        protected Contact CurrentContact { get; set; }

        protected SortedDictionary<string, Action<ContactMethod>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ContactMethod>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "ABCCB357-8841-4679-A3EC-8A531257123D";
        #endregion Fields
    }
}
