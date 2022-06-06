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
    public class ApiPaEmployeeDeductionEmployerController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/costs/employer/{id?}", typeof(DeductEmployer))]
        public async Task<IHttpActionResult> Get(string employeeId, string id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/costs/employer/{id?}", typeof(DeductEmployer))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/costs/employer/{id?}", typeof(DeductEmployer))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, id));
        }         

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/costs/employer/{id}", typeof(DeductEmployer))]
        public async Task Delete(string employeeId, string id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion  Web Methods

        #region Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add("DedCodeId", this.CodePropertyChanged);

            this.PropertyDictionary.Add(DeductBase.Columns.DeductionCodeId.ToString(), (entity) => {
                if (entity.DeductionCode != null)
                    entity.CalcOnGross = entity.DeductionCode.CalcOnGross;
            });
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is DeductEmployer deductEmployer)
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
            if (args.FieldName == "DedCodeId" && args.Entity is DeductEmployer entity)
            {
                entity.DeductionCodeId = EntityProvider.GetEntityList<DeductCode, DeductCodeProvider>(this.CompId, null, null).Find(x => x.DeductionCode == args.ActualValue.ToString()).Id;
            }
        }
        #endregion

        protected virtual async Task<EntityList<DeductEmployer>> Load(string employeeId, string id)
        {
            if(this.DeductEmployerList == null || !this.DeductEmployerList.Exists(i => StringHelper.AreEqual(i.DeductionCode.DeductionCode,id)))
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                var list = await this.Provider.Load<Employee>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);

                if (list.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee ID '{0}' could not be found.", employeeId));

                this.CurrentEmployee = list[0];
                this.DeductEmployerList = this.CurrentEmployee.Detail?.EmployerDeductions;

                await this.FilterEntityListAsync(this.DeductEmployerList, FunctionID);
            }

            if (!string.IsNullOrEmpty(id))
                return this.DeductEmployerList?.FindAll(x => x.DeductionCode.DeductionCode == id);

            return this.DeductEmployerList;
        }

        private async Task<DeductEmployer> Find(string employeeId, string id)
        {
            var list = await this.Load(employeeId, id);
            return list?.Find(x => x.DeductionCode.DeductionCode == id);
        }

        protected virtual async Task<List<DeductEmployer>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Cost Code ID is provided along with more than one record.");

            var entityList = new List<DeductEmployer>();
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

        protected virtual async Task<DeductEmployer> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, string id = null)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.DedCodeId) || bodyItem.DedCodeId == null)
                bodyItem.DedCodeId = code;
            else
                code = bodyItem.DedCodeId;

            var entity = await this.Find(employeeId, code);

            if (isCreate)
            {
                if (entity != null && !string.IsNullOrEmpty(code))
                    return entity;

                entity = CurrentEmployee?.Detail?.EmployerDeductions?.AddNew();
                entity.SetDefaults();
                entity.SeqNum = "1";
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Cost Code ID {0} could not be found on Employee ID '{1}'.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        private async Task MarkToDelete(string employeeId, string id)
        {
            var entity = await this.Find(employeeId, id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Cost Code '{0}' for Employee '{1}' does not exist.", id, employeeId));

            this.DeductEmployerList.Remove(entity);
            this.Provider?.Update(CompId);

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
            Action<DeductEmployer> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as DeductEmployer);
        }
        #endregion Event Handlers

        #region Properties
        private Employee CurrentEmployee { get; set; }
        protected EntityList<DeductEmployer> DeductEmployerList { get; set; }
        private EmployeeProvider Provider { get; } = new EmployeeProvider();
        protected SortedDictionary<string, Action<DeductEmployer>> PropertyDictionary { get; } = new SortedDictionary<string, Action<DeductEmployer>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "38f9c3bf-f0ef-41a7-8d9a-1cb612c9e648";
        #endregion Fields
    }
}
