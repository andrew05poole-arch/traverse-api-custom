#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Contacts.Controllers
{
    public class ApiSalesRepController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "salesrep/{id?}", typeof(SalesRep))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "salesrep/{id?}", typeof(SalesRep))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "salesrep/{id?}", typeof(SalesRep))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "salesrep/{id}", typeof(SalesRep))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<SalesRep>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.SalesRepId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<SalesRep>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<SalesRep.Columns>();
                    builder.AppendEquals(SalesRep.Columns.SalesRepId, id);
                    var list = new SalesRepProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<SalesRep> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.SalesRepId, id, false));
        }

        protected virtual async Task<List<SalesRep>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Sales Rep ID is provided along with more than one record.");

            var entityList = new List<SalesRep>();
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

        protected virtual async Task<SalesRep> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.SalesRepId) || string.IsNullOrWhiteSpace(bodyItem.SalesRepId))
                bodyItem.SalesRepId = code;
            else
                code = bodyItem.SalesRepId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new SalesRep(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Sales Rep '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Sales Rep '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailRecords")
            {
                if (((SalesRep)args.ParentObject).IsNew)
                    return this.CreateCommission((SalesRep)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateCommission((SalesRep)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        protected virtual SalesRepCommission UpdateCommission(SalesRep parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("ID is required.");

            SalesRepCommission entity = parent.DetailRecords.Find(x => x.Id == (int)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Commission record {0} for Sales Rep '{1}' could not be found.", bodyItem.Id, parent.SalesRepId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = CommissionUpdateComplete;
            entity.PropertyChanged += CommissionEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual SalesRepCommission CreateCommission(SalesRep parent, dynamic bodyItem)
        {
            SalesRepCommission entity = parent.DetailRecords.AddNew();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = CommissionUpdateComplete;
            entity.PropertyChanged += CommissionEntity_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void CommissionUpdateComplete(object entityObject)
        {
            var entity = entityObject as SalesRepCommission;
            entity.PropertyChanged -= CommissionEntity_PropertyChanged;
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
            Action<SalesRep> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as SalesRep);
        }

        private void CommissionEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<SalesRepCommission> action = null;
            if (CommissionPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as SalesRepCommission);
        }
        #endregion Event Handlers

        #region Properties
        protected SalesRepProvider Provider { get; } = new SalesRepProvider();

        protected SortedDictionary<string, Action<SalesRep>> PropertyDictionary { get; } = new SortedDictionary<string, Action<SalesRep>>();

        protected SortedDictionary<string, Action<SalesRepCommission>> CommissionPropertyDictionary { get; } = new SortedDictionary<string, Action<SalesRepCommission>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "5FA0617B-C7E1-4D69-B801-B88E2C351283";
        #endregion Fields
    }
}
