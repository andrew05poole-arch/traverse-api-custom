#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Manufacturing.Controllers
{
    public class ApiMrLaborTypeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "labortype/{id?}", typeof(Labor))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "labortype/{id?}", typeof(Labor))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "labortype/{id?}", typeof(Labor))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "labortype/{id}", typeof(Labor))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<Labor>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.LaborTypeId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Labor>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<LaborBase.Columns>();
                    builder.AppendEquals(LaborBase.Columns.LaborTypeId, id);
                    var list = new LaborProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Labor> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.LaborTypeId, id, false));
        }

        protected virtual async Task<List<Labor>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Labor Type ID is provided along with more than one record.");

            var entityList = new List<Labor>();
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

        protected virtual async Task<Labor> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborTypeId) || string.IsNullOrWhiteSpace(bodyItem.LaborTypeId))
                bodyItem.LaborTypeId = code;
            else
                code = bodyItem.LaborTypeId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Labor(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Labor Type ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Labor Type ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "EmployeeList")
            {
                if (((Labor)args.ParentObject).IsNew)
                    return this.CreateDetail((Labor)args.ParentObject, args.ItemModel);
            }
            return null;
        }

        #region Detail Update Methods
        protected virtual LaborTypeEmployee CreateDetail(Labor parent, dynamic bodyItem)
        {
            LaborTypeEmployee entity = parent.EmployeeList.AddNew();
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DetailUpdateComplete;
            entity.PropertyChanged += Detail_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DetailUpdateComplete(object entityObject)
        {
            var entity = entityObject as LaborTypeEmployee;
            entity.PropertyChanged -= Detail_PropertyChanged;
        }
        #endregion Detail Update Methods
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
            var entity = sender as Labor;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<Labor> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void Detail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<LaborTypeEmployee> action = null;
            if (DetailPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as LaborTypeEmployee);
        }
        #endregion Event Handlers

        #region Properties
        protected LaborProvider Provider { get; } = new LaborProvider();

        protected SortedDictionary<string, Action<Labor>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Labor>>();

        protected SortedDictionary<string, Action<LaborTypeEmployee>> DetailPropertyDictionary { get; } = new SortedDictionary<string, Action<LaborTypeEmployee>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "27CE2D14-8255-4A69-B784-F01A5446735C";
        #endregion Fields
    }
}
