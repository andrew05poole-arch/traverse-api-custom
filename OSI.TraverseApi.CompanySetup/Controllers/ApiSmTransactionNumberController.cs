#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.CompanySetup.Controllers
{
    public class ApiSmTransactionNumberController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transnumber/{id?}", typeof(TransactionNumber))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transnumber/{id?}", typeof(TransactionNumber))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transnumber/{id}/increment", typeof(TransactionNumber))]
        public async Task<IHttpActionResult> Increment([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is TransactionNumber transactionNumber)
            {
                if (StringHelper.AreEqual(args.FieldName, TransactionNumberBase.Columns.NextId.ToString(), false))
                {
                    if(CurrentNextId != 0)
                        args.ActualValue = CurrentNextId;
                }
            }
        }
        #endregion Overrides

        protected virtual async Task<EntityList<TransactionNumber>> Load(string id)
        {
            if (this.Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(id, i.FunctionId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<TransactionNumber>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TransactionNumberBase.Columns>();
                    builder.AppendEquals(TransactionNumberBase.Columns.FunctionId, id.ToString());
                    var list = new TransactionNumberProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<TransactionNumber> Find(string id)
        {
            var list = await Load(id);
            return list?.Find(x => StringHelper.AreEqual(x.FunctionId, id, false));
        }

        protected virtual async Task<List<TransactionNumber>> ProcessEditRequest(bool isIncrement, dynamic body, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Form Number ID is provided along with more than one record.");

            if (list.Length > 1 && isIncrement)
                throw new InvalidValueException("Only one form number can be increased per request");

            var entityList = new List<TransactionNumber>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isIncrement, item, id);
                if(!isIncrement)
                    this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await ValidateEntityListAsync(entityList);
            if (!isIncrement)
                this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<TransactionNumber> ProcessBodyItem(bool isIncrement, dynamic bodyItem, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.FunctionId) || string.IsNullOrWhiteSpace(bodyItem.FunctionId))
                bodyItem.FunctionId = code;
            else
                code = bodyItem.FunctionId;

            var entity = await this.Find(code);

            if (isIncrement && entity != null)
            {
                CurrentNextId = entity.NextId;
                entity.NextId = TransactionNumber.GetNextTransId(this.CompId, entity.FunctionId, new ValidateNextId(ValidateNextTransId), null) + 1;
                return entity;
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Form Number '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        private bool ValidateNextTransId(int nextNumber)
        {
            if (nextNumber < 0 || nextNumber > 99999999)
            {
                throw new InvalidValueException("Next transaction id is out of range.");
            }
            return true;
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
            Action<TransactionNumber> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransactionNumber);
        }
        #endregion Event Handlers

        #region Properties
        protected int CurrentNextId { get; set; }
        protected TransactionNumberProvider Provider { get; } = new TransactionNumberProvider();

        protected SortedDictionary<string, Action<TransactionNumber>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TransactionNumber>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "6f635363-f51a-47f8-aba2-6fdf5c836b85";
        #endregion
    }
}
