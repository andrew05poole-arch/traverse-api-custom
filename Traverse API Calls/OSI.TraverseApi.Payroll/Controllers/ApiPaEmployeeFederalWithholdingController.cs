#region Using Directives
using OSI.TraverseApi.Business;
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
#endregion Using Directives


namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaEmployeeFederalWithholdingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/federaltaxes/{id:int?}", typeof(WithholdFederal))]
        public async Task<IHttpActionResult> Get(string employeeId = null, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/federaltaxes/{id:int?}", typeof(WithholdFederal))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }
        #endregion  Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates() { }
        #endregion Override
        protected virtual async Task<EntityList<WithholdFederal>> Load(string employeeId, int? id)
        {
            if (CurrentWithholdFederal == null || CurrentWithholdFederal.Exists(i => i.Id != id))
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                var list = await this.Provider.Load<Employee>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty),PageNumber,PageSize);              

                if (list.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee '{0}' could not be found.", employeeId));

                CurrentWithholdFederal = list[0].Detail?.FederalWithholdingCodes;
                await this.FilterEntityListAsync(CurrentWithholdFederal, FunctionID);
            }

            if (id.HasValue)
                return CurrentWithholdFederal?.FindAll(x => x.Id == id);

            return CurrentWithholdFederal;
        }

        protected virtual async Task<WithholdFederal> Find(string employeeId, int id)
        {
            var list = await Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<WithholdFederal>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Federal Tax ID is provided along with more than one record.");

            var entityList = new List<WithholdFederal>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, employeeId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<WithholdFederal> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || bodyItem.Id == null)
                bodyItem.Id = code;
            else
                code = Convert.ToInt32(bodyItem.Id);

            var entity = await this.Find(employeeId, code);

            if (entity == null)
                throw new InvalidValueException(string.Format("Federal Tax ID {0} not be found on Employee ID'{1}'.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
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
            Action<WithholdFederal> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as WithholdFederal);
        }
        #endregion Event Handlers

        #region Properties
        protected EntityList<WithholdFederal> CurrentWithholdFederal { get; set; }
        private EmployeeProvider Provider { get; } = new EmployeeProvider();
        protected SortedDictionary<string, Action<WithholdFederal>> PropertyDictionary { get; } = new SortedDictionary<string, Action<WithholdFederal>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "e5ea00e2-e5be-4214-8f1d-6bbd4abce64f";
        #endregion Fields
    }
}
