#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Tax;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion  Using Directives

namespace TRAVERSE.Web.API.Tax.Controllers
{
    public class ApiSmTaxLocationGroupsController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "taxgroup/{id?}", typeof(TaxGroup))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overriders
        protected override void AddPropertyDelegates() { }
        #endregion Overriders

        protected virtual async Task<EntityList<TaxGroup>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => StringHelper.AreEqual( i.TaxGrpId , id,false))))
            {
                if (id == null)
                    await Provider.Load<TaxGroup>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TaxGroupBase.Columns>();
                    builder.AppendEquals(TaxGroupBase.Columns.TaxGrpId, id.ToString());
                    var list = new TaxGroupProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items);
            }

            return this.Provider.Items;
        }
        #endregion Helper Methods

        #region Properties
        protected TaxGroupProvider Provider { get; } = new TaxGroupProvider();

        protected SortedDictionary<string, Action<TaxGroup>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TaxGroup>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "bbe0498a-69d1-45a1-9d2f-2bb92f192660";
        #endregion Fields
    }
}
