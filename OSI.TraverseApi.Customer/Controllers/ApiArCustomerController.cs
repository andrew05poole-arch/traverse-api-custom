#region Using Directives
using System;
using OSI.TraverseApi.Business;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Business.Tax;
using OSI.TraverseApi.Contacts.Models;
#endregion Using Directives 

namespace OSI.TraverseApi.Contacts.Controllers
{
    public class ApiArCustomerController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "customer/{id?}", typeof(Customer))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "customer/{id?}", typeof(Customer))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "customer/{id?}", typeof(Customer))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "customer/{id}", typeof(Customer))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //No special property changed events to set
        }

        protected virtual async Task<EntityList<Customer>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.CustId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Customer>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<Customer.Columns>();
                    builder.AppendEquals(Customer.Columns.CustId, id);
                    var list = new CustomerProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Customer> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.CustId, id, false));
        }

        protected virtual async Task<List<Customer>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Customer ID is provided along with more than one record.");

            var entityList = new List<Customer>();
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

        protected virtual async Task<Customer> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.CustId) || string.IsNullOrWhiteSpace(bodyItem.CustId))
                bodyItem.CustId = code;
            else
                code = bodyItem.CustId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Customer(this.CompId);
                entity.CustId = id;
                this.SetCustDefaultValues(entity);
                entity.SetDefaultValues();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Customer '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Customer '{0}' could not be found.", id));

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
            Action<Customer> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Customer);
        }

        public virtual void SetCustDefaultValues(Customer newCustomer)
        {
            newCustomer.Country = ConfigurationValueProvider.GetRule<string>("SM", "defaultcountry", this.CompId);
            newCustomer.CurrencyId = ConfigurationValueProvider.GetRule<string>("SM", "basecurrency", this.CompId);
            newCustomer.DistCode = Utility.GetDefaultDistCode(this.CompId);
            newCustomer.PmtMethod = Utility.GetDefaultPmtMethodId(this.CompId);
            newCustomer.TaxLocId = Utility.GetDefaultTaxGrpId(this.CompId);
            newCustomer.TermsCode = Utility.GetDefaultTermsCode(this.CompId);
            if (string.IsNullOrEmpty(newCustomer.TermsCode) && new TermsCodeProvider().Load(this.CompId).Count > 0)
            {
                newCustomer.TermsCode = new TermsCodeProvider().Load(this.CompId)[0].TermsCode;
            }
            if (string.IsNullOrEmpty(newCustomer.DistCode) && new DistributionCodeProvider().Load(this.CompId).Count > 0)
            {
                newCustomer.DistCode = new DistributionCodeProvider().Load(this.CompId)[0].DistCode;
            }
            if (string.IsNullOrEmpty(newCustomer.TaxLocId) && new TaxGroupProvider().Load(this.CompId).Count > 0)
            {
                newCustomer.TaxLocId = new TaxGroupProvider().Load(this.CompId)[0].TaxGrpId;
            }
            if (string.IsNullOrEmpty(newCustomer.PmtMethod) && new PaymentMethodProvider().Load(this.CompId).Count > 0)
            {
                newCustomer.PmtMethod = new PaymentMethodProvider().Load(this.CompId)[0].PmtMethodId;
            }
            newCustomer.Status = 0;
            newCustomer.StmtInvcCode = 3;
            newCustomer.AccountType = new Customer.AccountTypes?(Customer.AccountTypes.OpenInvoice);
        }
        #endregion Event Handlers

        #region Properties
        protected CustomerProvider Provider { get; } = new CustomerProvider();

        protected SortedDictionary<string, Action<Customer>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Customer>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "19432034-BF3E-411A-B410-8362ABBE4AA6";
        #endregion Fields
    }
}
