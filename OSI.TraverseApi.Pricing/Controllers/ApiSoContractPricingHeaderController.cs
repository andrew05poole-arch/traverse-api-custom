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
    public class ApiSoContractPricingHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "contractpricing/{id?}", typeof(PriceContractHeader))]
        public async Task<IHttpActionResult> Get( string id = null)
        {
            return Ok(await Load(id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Overrides

        protected virtual async Task<EntityList<PriceContractHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.ContractPriceId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    Provider.Load(this.CompId);
                else
                {
                    var builder = new SqlFilterBuilder<PriceContractHeaderBase.Columns>();
                    builder.AppendEquals(PriceContractHeaderBase.Columns.ContractPriceId, id);
                    var list = new PriceContractHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
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
        public const string FunctionID = "649a91db-dcd5-4095-9011-5e6f08b54e66";
        #endregion Fields
    }
}
