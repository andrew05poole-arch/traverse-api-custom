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
    public class ApiPcRatesController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "ratecode/{id?}", typeof(Rates))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "ratecode/{id?}", typeof(Rates))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "ratecode/{id?}", typeof(Rates))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "ratecode/{id}", typeof(Rates))]
        public async T.Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<Rates>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(i.RateId, id, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Rates>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<RatesBase.Columns>();
                    builder.AppendEquals(RatesBase.Columns.RateId, id);
                    var list = new RatesProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        private async Task<Rates> Find(string id)
        {
            var list = await Load(id);
            return list?.Find(x => StringHelper.AreEqual(x.RateId, id, false));
        }

        protected virtual async Task<List<Rates>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Rate ID is provided along with more than one record.");

            var entityList = new List<Rates>();
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

        protected virtual async Task<Rates> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.RateId) || string.IsNullOrWhiteSpace(bodyItem.RateId))
                bodyItem.RateId = code;
            else
                code = bodyItem.RateId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Rates(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Rate ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Rate ID '{0}' could not be found.", id));

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
            Action<Rates> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Rates);
        }
        #endregion Event Handlers

        #region Properties
        protected RatesProvider Provider { get; } = new RatesProvider();

        protected SortedDictionary<string, Action<Rates>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Rates>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "a052411f-915d-4cf4-b3df-1d778abc5f4e";
        #endregion Fields
    }
}
