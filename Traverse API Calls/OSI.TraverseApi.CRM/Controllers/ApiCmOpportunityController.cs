#region Using Directives
using OSI.TraverseApi.Business;
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
    public class ApiCmOpportunityController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "opportunity/{id:long?}", typeof(Opportunity))]
        public async Task<IHttpActionResult> Get(long? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "opportunity/{id:long?}", typeof(Opportunity))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, long? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "opportunity", typeof(Opportunity))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "opportunity/{id:long}", typeof(Opportunity))]
        public async T.Task Delete(long id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<Opportunity>> Load(long? id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.Id == id)))
            {
                if (id == null)
                    await Provider.Load<Opportunity>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<OpportunityBase.Columns>();
                    builder.AppendEquals(OpportunityBase.Columns.Id, id.ToString());
                    var list = new OpportunityProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<Opportunity> Find(long id)
        {
            var list = await Load(id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<Opportunity>> ProcessEditRequest(bool isCreate, dynamic body, long? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Opportunity ID is provided along with more than one record.");

            var entityList = new List<Opportunity>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Opportunity> ProcessBodyItem(bool isCreate, dynamic bodyItem, long? id)
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

                entity = new Opportunity(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Opportunity ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(long id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Opportunity ID '{0}' could not be found.", id));

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
            Action<Opportunity> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Opportunity);
        }
        #endregion Event Handlers

        #region Properties
        protected OpportunityProvider Provider { get; } = new OpportunityProvider();

        protected SortedDictionary<string, Action<Opportunity>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Opportunity>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "45616B3A-53EF-4BC4-9547-12E235348A58";
        #endregion Fields
    }
}
