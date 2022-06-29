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
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaEmployeeDeductionCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "deductcode/employee/{id:int?}", typeof(DeductCode))]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "deductcode/employee/{id:int?}", typeof(DeductCode))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, int? id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "deductcode/employee", typeof(DeductCode))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body, null));
        }

        [ApiRoute(FunctionID, 2f, "deductcode/employee/{id:int}", typeof(DeductCode))]
        public async Task Delete(int id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<DeductCode>> Load(int? id)
        {
            if ((Provider.Items.Count <= 0) || (id != null && !Provider.Items.Exists(i => i.Id == id)))
            {
                var builder = new SqlFilterBuilder<DeductCodeBase.Columns>();
                builder.AppendEquals(DeductCodeBase.Columns.EmployerPaid, "0");
                if (id == null)
                {
                    var list = await this.Provider.Load<DeductCode>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);
                }
                else
                {
                    builder.AppendEquals(DeductCodeBase.Columns.Id, id.ToString());
                    var list = new DeductCodeProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<DeductCode> Find(int id)
        {
            var list = await Load(id);
            return list?.Find(x => x.Id == id);
        }

        protected virtual async Task<List<DeductCode>> ProcessEditRequest(bool isCreate, dynamic body, int? id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. Deduction Code ID is provided along with more than one record.");

            var entityList = new List<DeductCode>();
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

        protected virtual async Task<DeductCode> ProcessBodyItem(bool isCreate, dynamic bodyItem, int? id)
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

                entity = new DeductCode(this.CompId);
                entity.EmployerPaid = Convert.ToBoolean(0);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Deduction Code ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(int id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Deduction Code ID '{0}' could not be found.", id));
       
            this.Provider.Items?.Remove(entity);
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
            Action<DeductCode> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as DeductCode);
        }
        #endregion Event Handlers

        #region Properties
        protected DeductCodeProvider Provider { get; } = new DeductCodeProvider();

        protected SortedDictionary<string, Action<DeductCode>> PropertyDictionary { get; } = new SortedDictionary<string, Action<DeductCode>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "728CE45B-BAFD-4507-89FC-F47801E1887B";
        #endregion Fields
    }
}
