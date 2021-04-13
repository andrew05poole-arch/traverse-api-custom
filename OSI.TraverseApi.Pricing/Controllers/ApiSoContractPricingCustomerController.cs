#region Using Directives 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Pricing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Pricing.Controllers
{
    public class ApiSoContractPricingCustomerController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "contractpricing/{contractpriceid}/customer/{id?}", typeof(PriceContractCustomer))]
        public async Task<IHttpActionResult> Get(string contractPriceId = null, string id = null)
        {
            return Ok(await this.Load(contractPriceId, id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
        }
        #endregion Overrides
        protected virtual async Task<EntityList<PriceContractCustomer>> Load(string contractPriceId, string id)
        {
            var list = this.CurrentPriceContract?.PriceContractCustomerList;

            if (this.CurrentPriceContract == null || this.CurrentPriceContract.ContractPriceId != contractPriceId)
            {
                var builder = new SqlFilterBuilder<PriceContractHeaderBase.Columns>();
                builder.AppendEquals(PriceContractHeaderBase.Columns.ContractPriceId, contractPriceId.ToString());
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items, ApiSoContractPricingHeaderController.FunctionID);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Contract Price ID '{0}' could not be found.", contractPriceId));

                this.CurrentPriceContract = Provider.Items[0];

                await FilterEntityListAsync(this.CurrentPriceContract.PriceContractItemList, FunctionID);

                list = this.CurrentPriceContract.PriceContractCustomerList;
            }

            if (!string.IsNullOrEmpty(id))
                return list.FindAll(PriceContractCustomerBase.Columns.CustId, id);

            return list;
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
            Action<PriceContractHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PriceContractHeader);
        }
        #endregion Event Handlers

        #region Properties
        protected PriceContractHeader CurrentPriceContract { get; set; }
        protected PriceContractHeaderProvider Provider { get; } = new PriceContractHeaderProvider();
        protected SortedDictionary<string, Action<PriceContractHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PriceContractHeader>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "cf9b2505-7e09-4c3f-8fdc-6d0b23eb2347";
        #endregion Fields
    }
}
