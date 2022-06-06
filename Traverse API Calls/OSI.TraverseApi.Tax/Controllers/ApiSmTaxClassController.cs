#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Tax;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion  Using Directives

namespace OSI.TraverseApi.Tax.Controllers
{
    public class ApiSmTaxClassController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "taxclass/{id?}", typeof(TaxClass))]
        public async Task<IHttpActionResult> Get(byte? id = null)
        {
            return Ok(await Load(id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overriders
        protected override void AddPropertyDelegates() { }
        #endregion Overriders

        protected virtual async Task<EntityList<TaxClass>> Load(byte? id)
        {
            if (Provider.Items.Count <= 0 || (id != null && !Provider.Items.Exists(i => i.TaxClassCode == id)))
            {
                if (id == null)
                    await Provider.Load<TaxClass>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<TaxClassBase.Columns>();
                    builder.AppendEquals(TaxClassBase.Columns.TaxClassCode, id.ToString());
                    var list = new TaxClassProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
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
            Action<TaxClass> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TaxClass);
        }
        #endregion Event Handler

        #region Properties
        private TaxClassProvider Provider { get; } = new TaxClassProvider();
        protected SortedDictionary<string, Action<TaxClass>> PropertyDictionary { get; } = new SortedDictionary<string, Action<TaxClass>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "2764c2ba-55d6-46e1-870f-756544da1fdf";
        #endregion Fields
    }
}
