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

namespace OSI.TraverseApi.AccountsReceivable.Controllers
{
    public class ApiPcPhaseCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "phase/{id?}", typeof(Phase))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "phase/{id?}", typeof(Phase))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "phase/{id?}", typeof(Phase))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "phase/{id}", typeof(Phase))]
        public async T.Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<Phase>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(i.PhaseId, id, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Phase>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<PhaseBase.Columns>();
                    builder.AppendEquals(PhaseBase.Columns.PhaseId, id);
                    var list = new PhaseProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        private async Task<Phase> Find(string id)
        {
            var list = await Load(id);
            return list?.Find(x => StringHelper.AreEqual(x.PhaseId, id, false));
        }

        protected virtual async Task<List<Phase>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Phase ID is provided along with more than one record.");

            var entityList = new List<Phase>();
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

        protected virtual async Task<Phase> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.PhaseId) || string.IsNullOrWhiteSpace(bodyItem.PhaseId))
                bodyItem.PhaseId = code;
            else
                code = bodyItem.PhaseId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Phase(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Phase ID '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Phase ID '{0}' could not be found.", id));

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
            Action<Phase> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Phase);
        }
        #endregion Event Handlers

        #region Properties
        protected PhaseProvider Provider { get; } = new PhaseProvider();

        protected SortedDictionary<string, Action<Phase>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Phase>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "4F29737D-10E3-4C39-BB6F-06D59C4EF62B";
        #endregion Fields
    }
}