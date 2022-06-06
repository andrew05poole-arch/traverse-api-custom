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
    public class ApiPaEmployeeLocalWithholdingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/localtaxes/{id:int?}", typeof(WithholdLocal))]
        public async Task<IHttpActionResult> Get(string employeeId = null, int? id = null)
        {
            return Ok(await this.Load(employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/localtaxes/{id:int?}", typeof(WithholdLocal))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string employeeId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, employeeId, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/localtaxes", typeof(WithholdLocal))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string employeeId = null)
        {
            return Ok(await ProcessEditRequest(true, body, employeeId, null));
        }

        [ApiRoute(FunctionID, 2f, "employee/{employeeid}/localtaxes/{id:int}", typeof(WithholdLocal))]
        public async Task Delete(string employeeId, int id)
        {
            await this.MarkToDelete(employeeId, id);
        }
        #endregion  Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates() 
        {
            PropertyDictionary.Add("DefaultWH",(entity) =>
            {
                if (CurrentWithholdLocal.Count > 0)
                {
                    foreach (WithholdLocal item in CurrentWithholdLocal)
                    {
                        if (item.TaxAuthorityId != entity.TaxAuthorityId)
                        {
                            item.DefaultWH = false;
                            if (!CurrentWithholdLocal.Contains(item))
                                CurrentWithholdLocal.Add(item);
                        }
                    }
                }
            });
        }
        #endregion Override
        protected virtual async Task<EntityList<WithholdLocal>> Load(string employeeId, int? id)
        {
            if (CurrentWithholdLocal == null || CurrentWithholdLocal.Exists(i => i.Id != id))
            {
                var builder = new SqlFilterBuilder<SM.EmployeeBase.Columns>();
                builder.AppendEquals(SM.EmployeeBase.Columns.EmployeeId, employeeId);
                var list = await this.Provider.Load<Employee>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);

                if (list.Count <= 0)
                    throw new InvalidValueException(string.Format("Employee '{0}' could not be found.", employeeId));

                CurrentEmployee = list[0];
                CurrentWithholdLocal = list[0].Detail?.LocalWithholdingCodes;
                await this.FilterEntityListAsync(CurrentWithholdLocal, FunctionID);
            }

            if (id.HasValue)
                return CurrentWithholdLocal?.FindAll(x => x.Id == id);

            return CurrentWithholdLocal;
        }

        protected virtual async Task<WithholdLocal> Find(string employeeId, int id)
        {
            var list = await Load(employeeId, id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<WithholdLocal>> ProcessEditRequest(bool isCreate, dynamic body, string employeeId, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id.HasValue)
                throw new InvalidValueException("Call is ambiguous. Local Tax ID is provided along with more than one record.");

            var entityList = new List<WithholdLocal>();
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

        protected virtual async Task<WithholdLocal> ProcessBodyItem(bool isCreate, dynamic bodyItem, string employeeId, int? id)
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

                entity = CurrentEmployee.Detail.LocalWithholdingCodes.AddNew() as WithholdLocal;
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Local Tax ID {0} could not be found on Employee ID'{1}'.", code, employeeId));

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
                throw new InvalidValueException(string.Format("Local Tax ID {0} could not be found on Employee ID'{1}'", id, employeeId));

            CurrentEmployee.Detail.LocalWithholdingCodes.Remove(entity);
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
            Action<WithholdLocal> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as WithholdLocal);
        }
        #endregion Event Handlers

        #region Properties
        private Employee CurrentEmployee { get; set; }
        protected EntityList<WithholdLocal> CurrentWithholdLocal { get; set; }
        private EmployeeProvider Provider { get; } = new EmployeeProvider();
        protected SortedDictionary<string, Action<WithholdLocal>> PropertyDictionary { get; } = new SortedDictionary<string, Action<WithholdLocal>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "76ab9209-d7f6-4ddf-b168-8f35ffc2c833";
        #endregion Fields
    }
}
