#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TraverseApi;
using T = System.Threading.Tasks;
#endregion Using Directives

namespace OSI.TraverseApi.ProjectCosting.Controllers
{
    public class ApiPcOhAllocCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "overheadallocation/{id?}", typeof(OverheadAlloc))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "overheadallocation/{id?}", typeof(OverheadAlloc))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "overheadallocation/{id?}", typeof(OverheadAlloc))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "overheadallocation/{id}", typeof(OverheadAlloc))]
        public async T.Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<OverheadAlloc>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(i.OhAllCode, id, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<OverheadAlloc>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<OverheadAllocBase.Columns>();
                    builder.AppendEquals(OverheadAllocBase.Columns.OhAllCode, id);
                    var list = new OhAllocProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        private async Task<OverheadAlloc> Find(string id)
        {
            var list = await Load(id);
            return list?.Find(x => StringHelper.AreEqual(x.OhAllCode, id, false));
        }

        protected virtual async Task<List<OverheadAlloc>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Overhead Allocation ID is provided along with more than one record.");

            var entityList = new List<OverheadAlloc>();
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

        protected virtual async Task<OverheadAlloc> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.OhAllCode) || string.IsNullOrWhiteSpace(bodyItem.OhAllCode))
                bodyItem.OhAllCode = code;
            else
                code = bodyItem.OhAllCode;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new OverheadAlloc(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Overhead Allocation ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Overhead Allocation ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
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
            Action<OverheadAlloc> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as OverheadAlloc);
        }
        #endregion Event Handlers

        #region Properties
        protected OhAllocProvider Provider { get; } = new OhAllocProvider();

        protected SortedDictionary<string, Action<OverheadAlloc>> PropertyDictionary { get; } = new SortedDictionary<string, Action<OverheadAlloc>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "8540C0DD-F269-4EB5-8613-3D2A04F2C646";
        #endregion Fields
    }
}
