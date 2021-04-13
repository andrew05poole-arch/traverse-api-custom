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
#endregion  Using Directives

namespace OSI.TraverseApi.Pricing.Controllers
{
    public class ApiSoContractPricingDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "contractpricing/{contractpriceid}/detail", typeof(PriceContractHeader))]
        public async Task<IHttpActionResult> Get(string contractPriceId = null)
        {
            return Ok(await Load(contractPriceId));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<PriceContractDetail>> Load(string contractPriceId)
        {
            var list = this.CurrentPriceContract?.ContractPricingDetailList;

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

                list = this.CurrentPriceContract.ContractPricingDetailList;
            }

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
        public const string FunctionID = "2ce4da51-92d4-48a9-91d5-222292d607bd";
        #endregion Fields
    }
}
