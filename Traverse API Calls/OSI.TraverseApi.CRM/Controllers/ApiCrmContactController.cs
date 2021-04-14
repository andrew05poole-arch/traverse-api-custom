#region Using Directives
using OSI.TraverseApi.Business;
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
#endregion Using Directives

namespace OSI.TraverseApi.CRM.Controllers
{
    public class ApiCrmContactController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "contact/{id:long?}", typeof(Contact))]
        public async Task<IHttpActionResult> Get(long? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "contact/{id:long?}", typeof(Contact))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "contact", typeof(Contact))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "contact/{id:long}", typeof(Contact))]
        public async T.Task Delete(long id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() 
        {
            ContactMethodPropertyDictionary.Add(ContactMethodBase.Columns.TypeId.ToString(), (entity) =>
            {
                if ((CurrentContact?.ContactMethodList?.FindAll(x => x.TypeId == entity.TypeId) as EntityList<ContactMethod>)?.Count > 1)
                    throw new InvalidValueException("Contact Method already exists.");
            });
        }
        #endregion Overrides
        protected virtual async Task<EntityList<Contact>> Load(long? id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => id == i.Id)))
            {
                if (id == null)
                    await Provider.Load<Contact>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<ContactBase.Columns>();
                    builder.AppendEquals(ContactBase.Columns.Id, id.ToString());
                    var list = new ContactProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);

                    if (list.Count > 0)
                        CurrentContact = list[0];
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Contact> Find(long id)
        {
            var list = await Load(id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<Contact>> ProcessEditRequest(bool isCreate, dynamic body, long? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Contact ID is provided along with more than one record.");

            var entityList = new List<Contact>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }
            await ValidateEntityListAsync(entityList);
            Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Contact> ProcessBodyItem(bool isCreate, dynamic bodyItem, long? id)
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

                entity = new Contact(this.CompId);
                entity.SetDefaults();
                this.CurrentContact = entity;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Contact ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Contact ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "ContactMethodList")
            {
                if (((Contact)args.ParentObject).IsNew)
                    return this.CreateContactMethod((Contact)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateContactMethod((Contact)args.ParentObject, args.ItemModel);
            }
            else if (args.PropertyName == "AddressList")
            {
                if (((Contact)args.ParentObject).IsNew)
                    return this.CreateContactAddress((Contact)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateContactAddress((Contact)args.ParentObject, args.ItemModel);
            }
                return null;
        }

        #region Contact Method Method
        protected virtual ContactMethod UpdateContactMethod(Contact parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Contact Method ID is required.");

            this.FilterEntityList(parent.ContactMethodList, ApiCmContactMethodController.FunctionID);
            ContactMethod entity = parent?.ContactMethodList?.Find(ContactMethodBase.Columns.Id, (long)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Contact Method ID {0} could not be found on Contact ID'{1}'", bodyItem.Id, parent.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ContactMethodUpdateComplete;
            entity.PropertyChanged += ContactMethodEntity_PropertyChanged;


            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual ContactMethod CreateContactMethod(Contact parent, dynamic bodyItem)
        {
            ContactMethod entity = parent.ContactMethodList.AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ContactMethodUpdateComplete;
            entity.PropertyChanged += ContactMethodEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void ContactMethodUpdateComplete(object entityObject)
        {
            var entity = entityObject as ContactMethod;
            entity.PropertyChanged -= ContactMethodEntity_PropertyChanged;
        }
        #endregion Contact Method Method

        #region Address Method Method
        protected virtual ContactAddress UpdateContactAddress(Contact parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Contact Address ID is required.");

            this.FilterEntityList(parent.AddressList, ApiCmContactAddressController.FunctionID);
            ContactAddress entity = parent?.AddressList?.Find(ContactAddressBase.Columns.Id, (long)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Contact Address ID {0} could not be found on Contact ID'{1}'", bodyItem.Id, parent.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ContactAddressUpdateComplete;
            entity.PropertyChanged += ContactAddressEntity_PropertyChanged;


            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual ContactAddress CreateContactAddress(Contact parent, dynamic bodyItem)
        {
            ContactAddress entity = parent.AddressList.AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ContactAddressUpdateComplete;
            entity.PropertyChanged += ContactAddressEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void ContactAddressUpdateComplete(object entityObject)
        {
            var entity = entityObject as ContactAddress;
            entity.PropertyChanged -= ContactAddressEntity_PropertyChanged;
        }
        #endregion Address Method Method
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
            Action<Contact> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Contact);
        }

        private void ContactMethodEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ContactMethod> action = null;
            if (ContactMethodPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ContactMethod);
        }

        private void ContactAddressEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ContactAddress> action = null;
            if (ContactAddressPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ContactAddress);
        }
        #endregion Event Handlers

        #region Properties
        protected Contact CurrentContact { get; set; }
        protected ContactProvider Provider { get; } = new ContactProvider();
        protected SortedDictionary<string, Action<Contact>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Contact>>();
        protected SortedDictionary<string, Action<ContactMethod>> ContactMethodPropertyDictionary { get; } = new SortedDictionary<string, Action<ContactMethod>>();
        protected SortedDictionary<string, Action<ContactAddress>> ContactAddressPropertyDictionary { get; } = new SortedDictionary<string, Action<ContactAddress>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "12DB014A-92F5-4C3B-9F9D-A8B897C43F38";
        #endregion Fields
    }
}
