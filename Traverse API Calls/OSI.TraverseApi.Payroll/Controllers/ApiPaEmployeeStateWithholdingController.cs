#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TraverseApi;
using SM = TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaEmployeeStateWithholdingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/statetaxes/{id:int?}", typeof(WithholdState))]
        public async Task<IHttpActionResult> Get(string employeeId = null, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/statetaxes/{id:int?}", typeof(WithholdState))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/statetaxes", typeof(WithholdState))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/statetaxes/{id:int}", typeof(WithholdState))]
        public async Task Delete(string employeeId, int id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion  Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add("DefaultWH", (entity) =>
            {
                if (CurrentWithholdState.Count > 0)
                {
                    foreach (WithholdState item in CurrentWithholdState)
                    {
                        if (item.TaxAuthorityId != entity.TaxAuthorityId)
                        {
                            item.DefaultWH = false;
                            if (!CurrentWithholdState.Contains(item))
                                CurrentWithholdState.Add(item);
                        }
                    }
                }
            });
        }
        #endregion Override
        protected virtual async Task<EntityList<WithholdState>> Load(string employeeId, int? id)
        {
            if (CurrentWithholdState == null || CurrentWithholdState.Exists(i => i.Id != id))
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                var list = await this.Provider.Load<Employee>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);

                if (list.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee '{0}' could not be found.", employeeId));

                CurrentEmployee = list[0];
                CurrentWithholdState = list[0].Detail?.StateWithholdingCodes;
                await this.FilterEntityListAsync(CurrentWithholdState, FunctionID);
            }

            if (id.HasValue)
                return CurrentWithholdState?.FindAll(x => x.Id == id);

            return CurrentWithholdState;
        }

        protected virtual async Task<WithholdState> Find(string employeeId, int id)
        {
            var list = await Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<WithholdState>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. State Tax ID is provided along with more than one record.");

            var entityList = new List<WithholdState>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, employeeId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);

                await this.ValidateEntityListAsync(entityList);
                this.Provider?.Update(this.CompId);
            }        

            return entityList;
        }

        protected virtual async Task<WithholdState> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(employeeId, code);

            if (isCreate)
            {
                if (entity != null && code != 0)
                    return entity;

                entity = CurrentEmployee.Detail.StateWithholdingCodes.AddNew() as WithholdState;
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("State Tax ID {0} could not be found on Employee ID'{1}'.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        private async Task MarkToDelete(string employeeId, int id)
        {
            var entity = await this.Find(employeeId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("State Tax ID {0} could not be found on Employee ID'{1}'", id, employeeId));

            CurrentEmployee.Detail.StateWithholdingCodes.Remove(entity);
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
            Action<WithholdState> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as WithholdState);
        }
        #endregion Event Handlers

        #region Properties
        private Employee CurrentEmployee { get; set; }
        protected EntityList<WithholdState> CurrentWithholdState { get; set; }
        private EmployeeProvider Provider { get; } = new EmployeeProvider();
        protected SortedDictionary<string, Action<WithholdState>> PropertyDictionary { get; } = new SortedDictionary<string, Action<WithholdState>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "08a4911e-ca66-4009-982c-7538524db173";
        #endregion Fields
    }
}
