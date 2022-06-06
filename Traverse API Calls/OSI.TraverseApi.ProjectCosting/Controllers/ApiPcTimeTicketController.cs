#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.ProjectCosting;
using TRAVERSE.Core;
using TraverseApi;
using T = System.Threading.Tasks;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.ProjectCosting.Controllers
{
    public class ApiPcTimeTicketController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "timeticket/{id:int?}", typeof(TimeTicket))]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "timeticket/{id:int?}", typeof(TimeTicket))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "timeticket/{id:int?}", typeof(TimeTicket))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "timeticket/{id:int}", typeof(TimeTicket))]
        public async T.Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(TimeTicketBase.Columns.EmployeeId.ToString(), (entity) => entity.SetEmployeeDefaults());
            PropertyDictionary.Add(TimeTicketBase.Columns.ProjectDetailId.ToString(), (entity) => entity.SetProjectDefaults());
            PropertyDictionary.Add(TimeTicketBase.Columns.RateId.ToString(), (entity) => entity.SetEmployeeRateDefaults());
            PropertyDictionary.Add(TimeTicketBase.Columns.TransDate.ToString(), TransDatePropertyChanged);
        }

        protected virtual async Task<EntityList<TimeTicket>> Load(int? id)
        {
            if (Provider.Items.Count <= 0 || !Provider.Items.Exists(i => i.Id == id))
            {
                if (!id.HasValue)
                    await Provider.Load<TimeTicket>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TimeTicketBase.Columns>();
                    builder.AppendEquals(TimeTicketBase.Columns.Id, id.ToString());
                    var list = new TimeTicketProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }
                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        private async Task<TimeTicket> Find(int id)
        {
            var list = await Load(id);
            return list.Find(x => x.Id == id);
        }

        protected virtual async Task<List<TimeTicket>> ProcessEditRequest(bool isCreate, dynamic body, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Time Ticket ID is provided along with more than one record.");

            var entityList = new List<TimeTicket>();
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

        protected virtual async Task<TimeTicket> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
        {
            int code = id.GetValueOrDefault();
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = new TimeTicket(this.CompId);
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Time Ticket ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async T.Task MarkToDelete(int id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Time Ticket ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void TransDatePropertyChanged(TimeTicket entity)
        {
            short period = (short)entity.TransDate.Month;
            short year = (short)entity.TransDate.Year;

            PeriodConversion fiscalPeriod = PeriodConversion.GetFiscalPeriod(period, year);
            if (fiscalPeriod != null)
            {
                if (fiscalPeriod.ClosedGL.Value)
                    AddWarnings(string.Format("Period {0} is closed for fiscal year {1}.", period, year));
            }
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
            Action<TimeTicket> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TimeTicket);
        }
        #endregion Event Handlers

        #region Properties
        protected TimeTicketProvider Provider { get; } = new TimeTicketProvider();

        protected SortedDictionary<string, Action<TimeTicket>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TimeTicket>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "7358E34E-FB2F-428C-B110-F595CB4EB69D";
        #endregion Fields
    }
}
