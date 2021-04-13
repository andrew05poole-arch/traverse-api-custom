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
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaEmployeeBonusController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/bonus/{id:int?}", typeof(Bonus))]
        public async Task<IHttpActionResult> Get(string employeeId, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/bonus/{id:int?}", typeof(Bonus))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/bonus", typeof(Bonus))]
        public async Task<IHttpActionResult> Post([FromBody] dynamic body, string employeeId)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/bonus/{id:int}", typeof(Bonus))]
        public async Task Delete(string employeeId, int id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion

        protected virtual async Task<EntityList<Bonus>> Load(string employeeId, int? id)
        {
            var list = this.CurrentEmployee?.Detail?.Bonuses;

            if (this.CurrentEmployee == null || this.CurrentEmployee.Detail.EmployeeId != employeeId)
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(this.Provider.Items);

                if (this.Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee ID '{0}' could not be found.", employeeId));

                this.CurrentEmployee = this.Provider.Items[0];

                list = CurrentEmployee.Detail.Bonuses;
                await this.FilterEntityListAsync(list, FunctionID);
            }

            if (id.HasValue)
                return list.FindAll(BonusBase.Columns.Id, id);

            return list;
        }

        protected virtual async Task<Bonus> Find(string employeeId, int id)
        {
            var list = await this.Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<Bonus>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Bonus ID is provided along with more than one record.");

            var entityList = new List<Bonus>();
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

        protected virtual async Task<Bonus> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
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

                entity = CurrentEmployee?.Detail?.Bonuses?.AddNew();
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new NothingToProcessException(string.Format("Bonus ID '{0}' for Employee '{1}' does not exist.", code, employeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string employeeId, int id)
        {
            var bonus = await this.Find(employeeId, id);
            if (bonus == null)
                throw new NothingToProcessException(string.Format("Bonus ID '{1}' for Employee ID '{0}' does not exist.", employeeId, id));

            this.CurrentEmployee.Detail?.Bonuses.Remove(bonus);
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
            Action<Bonus> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Bonus);
        }
        #endregion Event Handlers

        #region Properties
        protected EmployeeProvider Provider { get; } = new EmployeeProvider();

        protected Employee CurrentEmployee { get; set; }

        protected SortedDictionary<string, Action<Bonus>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Bonus>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "1BEB4AC3-D76D-4633-8EE7-5570F06279FA";
        #endregion
    }
}