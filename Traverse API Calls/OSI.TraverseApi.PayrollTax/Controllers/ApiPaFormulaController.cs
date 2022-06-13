#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PayrollTax.Controllers
{
    public class ApiPaFormulaController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "formula/{id?}", typeof(FormulaHeader))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates() { }
        #endregion Override

        protected virtual async Task<EntityList<FormulaHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.Id, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<FormulaHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<FormulaHeaderBase.Columns>();
                    builder.AppendEquals(FormulaHeaderBase.Columns.Id, id);
                    var list = new FormulaHeaderProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items, FunctionID);
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
            Action<FormulaHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as FormulaHeader);
        }
        #endregion Event Handlers

        #region Properties
        protected FormulaHeaderProvider Provider { get; } = new FormulaHeaderProvider();

        protected SortedDictionary<string, Action<FormulaHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<FormulaHeader>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "66da7c82-7a63-4b7b-b375-cebc04f2ff42";
        #endregion Fields 
    }
}
