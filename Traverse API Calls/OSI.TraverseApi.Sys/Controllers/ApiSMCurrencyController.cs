#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Sys;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.Sys.Controllers
{
    public class ApiSmCurrencyController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "currency/{id?}", typeof(Currency))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overriders
        protected override void AddPropertyDelegates() { }
        #endregion Overriders

        protected virtual async Task<EntityList<Currency>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.CurrId == id)))
            {
                if (id == null)
                    await Provider.Load<Currency>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<CurrencyBase.Columns>();
                    builder.AppendEquals(CurrencyBase.Columns.CurrId, id.ToString());
                    var list = new CurrencyProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items);
            }

            return this.Provider.Items;
        }
        #endregion Helper Methods

        #region Event Handler
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Currency> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Currency);
        }
        #endregion Event Handler

        #region Properties
        private CurrencyProvider Provider { get; } = new CurrencyProvider();
        protected SortedDictionary<string, Action<Currency>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Currency>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "d5e4c2b9-3daf-4f40-abce-dc5f6eb8b238";
        #endregion Fields
    }
}
