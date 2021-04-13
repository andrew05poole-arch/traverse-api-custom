#region Using Directives
using OSI.TraverseApi.Pricing.Models;
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
    public class ApiSoUpdateContractPricingController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "updatecontracts", typeof(ContractUpdate))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(true, body));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates(){}
        #endregion Overrides

        protected virtual async Task<List<ContractUpdate>> ProcessEditRequest(bool isCreate, dynamic body)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. Update Contract is provided along with more than one record.");

          Task.Run(()=> ExecuteUpdateContractPricing(body)); 

            return null;
        }

        private void ExecuteUpdateContractPricing(dynamic body)
        {
            UpdateContractPricing processEngine = ProcessBase.LoadProcessEngine<UpdateContractPricing>(this.CompId);
            processEngine.ContractList.AddRange(EntityProvider.GetEntityList<PriceContractHeader, PriceContractHeaderProvider>(this.CompId, new FilterCriteria(), null));
            processEngine.UpdateCustomers = (!ApiUserSkipped.IsApiUserSkipped(body.UpdateCustomers)) ? body.UpdateCustomer : false;
            processEngine.UpdateItems = (!ApiUserSkipped.IsApiUserSkipped(body.UpdateItems)) ? body.UpdateItems : false;
            processEngine.Comments = (!ApiUserSkipped.IsApiUserSkipped(body.Comments)) ? body.Comments : string.Empty;
            processEngine.Execute(null);
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
            Action<ContractUpdate> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ContractUpdate);
        }
        #endregion Event Handlers

        #region Properties
        protected SortedDictionary<string, Action<ContractUpdate>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ContractUpdate>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "7b918194-11e4-49cc-9934-1ff9ba5d2aea";
        #endregion Fields
    }
}
