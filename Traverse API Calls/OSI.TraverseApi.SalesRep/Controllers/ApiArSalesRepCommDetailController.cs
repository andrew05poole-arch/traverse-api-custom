#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Contacts.Controllers
{
    public class ApiArSalesRepCommDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "salesrep/{salesrepid}/commission/{id:int?}", typeof(SalesRepCommission))]
        public async Task<IHttpActionResult> Get(string salesrepid, int? id = null)
        {
            return Ok(await this.Load(salesrepid, id));
        }

        [ApiRoute(FunctionID, 2f, "salesrep/{salesrepid}/commission/{id:int?}", typeof(SalesRepCommission))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string salesrepid, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, salesrepid, id));
        }

        [ApiRoute(FunctionID, 2f, "salesrep/{salesrepid}/commission", typeof(SalesRepCommission))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string salesrepid)
        {
            return Ok(await ProcessEditRequest(true, body, salesrepid, null));
        }

        [ApiRoute(FunctionID, 2f, "salesrep/{salesrepid}/commission/{id:int}", typeof(SalesRepCommission))]
        public async Task Delete(string salesrepId, int id)
        {
            await this.MarkToDelete(salesrepId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //No special property changed events to set
        }

        protected virtual async Task Load(string id)
        {
            var builder = new SqlFilterBuilder<SalesRepBase.Columns>();
            builder.AppendEquals(SalesRepBase.Columns.SalesRepId, id);

            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            await this.FilterEntityListAsync(Provider.Items, ApiSalesRepController.FunctionID);
            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("Sales Rep '{0}' could not be found.", id));

            CurrentSalesRep = Provider.Items[0];
        }

        protected virtual async Task<EntityList<SalesRepCommission>> Load(string salesRepId, int? id)
        {
            var list = CurrentSalesRep?.DetailRecords;

            if (CurrentSalesRep == null || !StringHelper.AreEqual(CurrentSalesRep.SalesRepId, salesRepId, false))
            {
                var builder = new SqlFilterBuilder<SalesRepBase.Columns>();
                builder.AppendEquals(SalesRepBase.Columns.SalesRepId, salesRepId);
                Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiSalesRepController.FunctionID);
                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Transaction '{0}' could not be found.", id));

                CurrentSalesRep = Provider.Items[0];

                list = CurrentSalesRep.DetailRecords;
                await this.FilterEntityListAsync(list);
            }

            if (id.HasValue)
                return list.FindAll(SalesRepCommission.Columns.Id, id.Value);

            return list;
        }

        protected virtual async Task<SalesRepCommission> Find(string salesRepId, int id)
        {
            var list = await Load(salesRepId, id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<SalesRepCommission>> ProcessEditRequest(bool isCreate, dynamic body, string salesRepId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Commission ID is provided along with more than one record.");

            var entityList = new List<SalesRepCommission>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, salesRepId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<SalesRepCommission> ProcessBodyItem(bool isCreate, dynamic bodyItem, string salesRepId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);
            
            var entity = await this.Find(salesRepId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentSalesRep.DetailRecords.AddNew();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Commssion record '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string salesRepId, int id)
        {
            var entity = await this.Find(salesRepId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Commssion record '{0}' could not be found.", id));

            CurrentSalesRep.DetailRecords.Remove(entity);
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
            Action<SalesRepCommission> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as SalesRepCommission);
        }
        #endregion Event Handlers

        #region Properties
        protected SalesRepProvider Provider { get; } = new SalesRepProvider();

        protected SalesRep CurrentSalesRep { get; set; }

        protected SortedDictionary<string, Action<SalesRepCommission>> PropertyDictionary { get; } = new SortedDictionary<string, Action<SalesRepCommission>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "DA2E1101-2613-41C8-BD1C-7E3B67301F70";
        #endregion Fields
    }
}
