#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Document.Controllers
{
    public class ApiSmAttachmentController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "attachment", typeof(Attachment))]
        public async Task<IHttpActionResult> Get([FromBody] dynamic body)
        {
            return Ok(await ProcessRequest(TypeRequest.Get, body));
        }

        [ApiRoute(FunctionID, 2f, "attachment", typeof(Attachment))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body)
        {
            return Ok(await ProcessRequest(TypeRequest.Edit, body));
        }

        [ApiRoute(FunctionID, 2f, "attachment", typeof(Attachment))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessRequest(TypeRequest.Create, body));
        }

        [ApiRoute(FunctionID, 2f, "attachment", typeof(Attachment))]
        public async Task Delete([FromBody] dynamic body)
        {
            await ProcessRequest(TypeRequest.Delete, body);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates(){}

        protected virtual async Task<EntityList<Attachment>> Load(dynamic bodyItem)
        {
            //This validation allows to use the result of the previous request to find an item from the next request 
            //if it exists in the list avoiding having to re-query to the database.
            if (Provider.Items.Count <= 0
            || !Provider.Items.Exists(i => StringHelper.AreEqual(bodyItem.Id.ToString(), i.Id.ToString(), false)))
            {
            var builder = new SqlFilterBuilder<AttachmentBase.Columns>();
                if (!ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                {
                    if (bodyItem.Id > 0)
                        builder.AppendEquals(AttachmentBase.Columns.Id, bodyItem.Id.ToString());
                }
            if (!ApiUserSkipped.IsApiUserSkipped(bodyItem.LinkKey))
                    if(!string.IsNullOrEmpty(bodyItem.LinkKey) && !string.IsNullOrWhiteSpace(bodyItem.LinkKey))
                    {
                        builder.AppendEquals(AttachmentBase.Columns.LinkKey, bodyItem.LinkKey);
                    }               
            if (!ApiUserSkipped.IsApiUserSkipped(bodyItem.LinkType))
                    if(!string.IsNullOrEmpty(bodyItem.LinkType) && !string.IsNullOrWhiteSpace(bodyItem.LinkType))
                    {
                        builder.AppendEquals(AttachmentBase.Columns.LinkType, bodyItem.LinkType);
                    }               
            var list = await Provider.Load<Attachment>(this.CompId, new FilterCriteria(builder.ToString(), String.Empty), PageNumber, PageSize);

            await this.FilterEntityListAsync(list);
            }

            // This validation allows locate a single record if it exist in the previous result avoid a new query to the database.
            if (!ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
            {
                if (bodyItem.Id > 0)
                    return Provider.Items.FindAll(x => StringHelper.AreEqual(x.Id.ToString(), bodyItem.Id.ToString(), false));
            }
            return Provider.Items;
        }

        protected virtual async Task<Attachment> Find(dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Attachment ID is necessary.");
            if (string.IsNullOrEmpty(bodyItem.Id.ToString()) || string.IsNullOrWhiteSpace(bodyItem.Id.ToString()))
                throw new InvalidValueException("A valid Attachment ID is necessary.");

            EntityList<Attachment> list = await Load(bodyItem);
            return list.Find(x => StringHelper.AreEqual(x.Id.ToString(), bodyItem.Id.ToString(),false));
        }

        protected virtual async Task<EntityList<Attachment>> ProcessRequest(TypeRequest typeRequest, dynamic body)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            var entityList = new EntityList<Attachment>();
            foreach (dynamic item in list)
            {
                if (typeRequest == TypeRequest.Get)
                {
                    EntityList<Attachment> entities = await this.Load(item);
                            entityList.AddRange(entities);
                }
                else
                {
                    if (typeRequest == TypeRequest.Delete)
                    {
                        var entity = await this.MarkToDelete(item);
                        if (!entityList.Contains(entity))
                            entityList.Add(entity);
                    }
                    else
                    {
                        var entity = await this.ProcessBodyItem(typeRequest, item);
                        if (!entityList.Contains(entity))
                            entityList.Add(entity);

                        await this.ValidateEntityListAsync(entityList);                      
                    }                   
                }                
            }
            this.Provider.Items.Clear();
            this.Provider.Items.AddRange(entityList);
            this.Provider?.Update(this.CompId);
            return entityList;
        }

        protected virtual async Task<Attachment> ProcessBodyItem(TypeRequest typeRequest, dynamic bodyItem)
        {
            Attachment entity = null;
            if (typeRequest != TypeRequest.Create)
                entity = await this.Find(bodyItem);
            else
            {
                if (entity != null)
                    return entity;

                entity = new Attachment(this.CompId);
            }
            if (entity == null)
                throw new InvalidValueException(string.Format("Attachment ID '{0}' could not be found.", bodyItem.Id));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task<Attachment> MarkToDelete(dynamic bodyItem)
        {
            Attachment entity = await this.Find(bodyItem);

            if (entity == null)
                throw new InvalidValueException(string.Format("Attachment ID '{0}' could not be found.", bodyItem.Id));
            entity.MarkToDelete();
            return entity;
        }

        public enum TypeRequest
        {
            Create = 1,
            Edit = 2,
            Delete = 3,
            Get = 4
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
            Action<Attachment> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Attachment);
        }
        #endregion  Event Handlers

        #region Properties
        protected AttachmentProvider Provider { get; } = new AttachmentProvider();

        protected SortedDictionary<string, Action<Attachment>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Attachment>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "9069fe16-8035-416f-babf-7c68d9f53cb7";
        #endregion Fields
    }
}
