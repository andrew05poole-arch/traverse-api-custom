#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Sys;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Sys.Controllers
{
    public class ApiSmCountryCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "country/{id?}", typeof(Country))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "countryiso/{id}", typeof(Country))]
        public async Task<IHttpActionResult> GetIso(string id = null)
        {
            return Ok(await LoadIso(id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overriders
        protected override void AddPropertyDelegates() { }
        #endregion Overriders

        protected virtual async Task<EntityList<Country>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.Country == id)))
            {
                if (id == null)
                    await Provider.Load<Country>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<CountryBase.Columns>();
                    builder.AppendEquals(CountryBase.Columns.Country, id.ToString());
                    var list = new CountryProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items);
            }

            return this.Provider.Items;
        }

        protected virtual async Task<EntityList<Country>> LoadIso(string id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.ISOCode == id)))
            {
                var builder = new SqlFilterBuilder<CountryBase.Columns>();
                builder.AppendEquals(CountryBase.Columns.ISOCode, id.ToString());
                var list = new CountryProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                this.Provider.Items.AddRange(list);
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
            Action<Country> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Country);
        }
        #endregion Event Handler

        #region Properties
        private CountryProvider Provider { get; } = new CountryProvider();
        protected SortedDictionary<string, Action<Country>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Country>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "9a955b63-165e-4efb-a9c5-95e99bff7965";
        #endregion Fields
    }
}
