#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.CompanySetup.Controllers
{
    public class ApiSmBankAccountController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bankaccount/{id?}", typeof(BankAccount))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "bankaccount/{id?}", typeof(BankAccount))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "bankaccount/{id?}", typeof(BankAccount))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "bankaccount/{id}", typeof(BankAccount))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            PropertyDictionary.Add(BankAccountBase.Columns.BankId.ToString(), BankIdPropertyChanged);
        }
        #endregion Overrides

        protected virtual async Task<EntityList<BankAccount>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.BankId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<BankAccount>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<BankAccountBase.Columns>();
                    builder.AppendEquals(BankAccountBase.Columns.BankId, id);
                    var list = new BankAccountProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        private async Task<BankAccount> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.BankId, id, false));
        }

        protected virtual async Task<List<BankAccount>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Bank Account is provided along with more than one record.");

            var entityList = new List<BankAccount>();
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

        protected virtual async Task<BankAccount> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.BankId) || string.IsNullOrWhiteSpace(bodyItem.BankId))
                bodyItem.BankId = code;
            else
                code = bodyItem.BankId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new BankAccount(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Bank Account '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Bank Account ID '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void BankIdPropertyChanged(BankAccount entity)
        {
            entity.Country = ConfigurationValueProvider.GetRule<string>("SM", "defaultcountry", this.CompId);
            entity.CurrencyId = ConfigurationValueProvider.GetRule<string>("SM", "basecurrency", this.CompId);
            entity.ACHFileName = "?_DDEPOSIT_* ";
            entity.SecurityCodePadLength = null;
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
            Action<BankAccount> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as BankAccount);
        }
        #endregion Event Handlers

        #region Properties
        protected BankAccountProvider Provider { get; } = new BankAccountProvider();
        protected SortedDictionary<string, Action<BankAccount>> PropertyDictionary { get; } = new SortedDictionary<string, Action<BankAccount>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "82F94046-E961-4F31-9BD6-58CFDF97E1C0";
        #endregion Fields
    }
}
