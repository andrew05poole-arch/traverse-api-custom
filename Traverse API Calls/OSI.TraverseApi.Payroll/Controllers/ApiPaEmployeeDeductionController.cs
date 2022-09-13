#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using SM = TRAVERSE.Business.CompanySetup;
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaEmployeeDeductionController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/deduction/employee/{id?}", typeof(DeductEmployee))]
        public async Task<IHttpActionResult> Get(string employeeId, string id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/deduction/employee/{id?}", typeof(DeductEmployee))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/deduction/employee/{id?}", typeof(DeductEmployee))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/deduction/employee/{id}", typeof(DeductEmployee))]
        public async Task Delete(string employeeId, string id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add("DedCodeId", this.CodePropertyChanged);

            this.PropertyDictionary.Add(DeductEmployee.Columns.DeductionCodeId.ToString(), this.DeductionCodeIdPropertyChanded);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is DeductEmployee deductEmployer)
            {
                if (StringHelper.AreEqual(args.FieldName, "DedCodeId", false))
                {
                    args.ActualValue = deductEmployer.DeductionCode?.DeductionCode;
                }
            }
        }
        #endregion

        #region Body Item Update Methods
        protected virtual void CodePropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "DedCodeId" && args.Entity is DeductEmployee entity)
            {
                entity.DeductionCodeId = EntityProvider.GetEntityList<DeductCode, DeductCodeProvider>(this.CompId, null, null).Find(x => x.DeductionCode == args.ActualValue.ToString()).Id;
            }
        }
        #endregion

        protected virtual async Task<EntityList<DeductEmployee>> Load(string employeeId, string id)
        {
            var list = this.CurrentEmployee?.Detail?.EmployeeDeductions;
            if (this.CurrentEmployee == null || !StringHelper.AreEqual(this.CurrentEmployee.Detail.EmployeeId, employeeId))
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(this.Provider.Items);

                if (this.Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee ID '{0}' could not be found.", employeeId));

                this.CurrentEmployee = this.Provider.Items[0];

                list = CurrentEmployee.Detail.EmployeeDeductions;
                await this.FilterEntityListAsync(list, FunctionID);
            }
            if (!string.IsNullOrEmpty(id))
                return list.FindAll(x => x.DeductionCode.DeductionCode == id);

            return list;
        }

        protected virtual async Task<DeductEmployee> Find(string employeeId, string id)
        {
            var list = await Load(employeeId, id);
            return list?.Find(x => x.DeductionCode.DeductionCode == id);
        }

        protected virtual async Task<List<DeductEmployee>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Deduction Code is provided along with more than one record.");

            var entityList = new List<DeductEmployee>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, employeeId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<DeductEmployee> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.DedCodeId) || bodyItem.DedCodeId == null)
                bodyItem.DedCodeId = code;
            else
                code = bodyItem.DedCodeId;

            var entity = await this.Find(employeeId, code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = CurrentEmployee?.Detail?.EmployeeDeductions?.AddNew();
                entity.SetDefaults();
                entity.SeqNum = "1";
            }
            else if (entity == null)
                throw new NothingToProcessException(string.Format("Deduction Code '{0}' for Employee '{1}' does not exist.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string employeeId, string id)
        {
            var deductEmployee = await this.Find(employeeId, id);

            if (deductEmployee == null)
                throw new NothingToProcessException(string.Format("Deduction Code '{0}' for Employee '{1}' does not exist.", id, employeeId));
            
            this.CurrentEmployee.Detail?.EmployeeDeductions.Remove(deductEmployee);
            this.Provider.Update(this.CompId);
        }        
        #endregion Helper Methods

        #region Update Methods
        protected virtual void DeductionCodeIdPropertyChanded(DeductEmployee entity)
        {
            if (entity.DeductionCode != null)
                entity.CalcOnGross = entity.DeductionCode.CalcOnGross;
        }
        #endregion

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<DeductEmployee> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as DeductEmployee);
        }
        #endregion Event Handlers

        #region Properties
        protected EmployeeProvider Provider { get; } = new EmployeeProvider();

        protected SortedDictionary<string, Action<DeductEmployee>> PropertyDictionary { get; } = new SortedDictionary<string, Action<DeductEmployee>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        protected Employee CurrentEmployee { get; set; }
        #endregion Properties

        #region Fields
        public const string FunctionID = "0e708eb0-4e71-4afb-a6c0-e3127f3ced60";
        #endregion
    }
}
