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
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaEarningCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "earningcode/{id?}", typeof(EarnCode))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "earningcode/{id?}", typeof(EarnCode))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "earningcode/{id?}", typeof(EarnCode))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "earningcode/{id}", typeof(EarnCode))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() 
        {
            PropertyDictionary.Add(EarnCodeBase.Columns.W2Box.ToString(), (entity) =>
            {
                if (!Utility.GetW2BoxList.ContainsKey(entity.W2Box))
                    throw new InvalidValueException("W-2 Box is invalid.");
            });
        }

        protected virtual async Task<EntityList<EarnCode>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.Id, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<EarnCode>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<EarnCodeBase.Columns>();
                    builder.AppendEquals(EarnCodeBase.Columns.Id, id);
                    var list = new EarnCodeProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<EarnCode> Find(string id)
        {
            var list = await Load(id);
            return list?.Find(x => StringHelper.AreEqual(x.Id, id, false));
        }

        protected virtual async Task<List<EarnCode>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Earn Code ID is provided along with more than one record.");

            var entityList = new List<EarnCode>();
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

        protected virtual async Task<EarnCode> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) || string.IsNullOrWhiteSpace(bodyItem.Id))
                bodyItem.Id = code;
            else
                code = bodyItem.Id;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new EarnCode(this.CompId);
                entity.IncludeInNet = true;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Earn Code ID '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;
            entity.PropertyChanged -= Entity_PropertyChanged;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Earn Code ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
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
            Action<EarnCode> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as EarnCode);
        }
        #endregion Event Handlers

        #region Properties
        protected EarnCodeProvider Provider { get; } = new EarnCodeProvider();

        protected SortedDictionary<string, Action<EarnCode>> PropertyDictionary { get; } = new SortedDictionary<string, Action<EarnCode>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "DBEE82A4-B9F7-4576-808C-67BABD681BB1";
        #endregion Fields
    }
}
