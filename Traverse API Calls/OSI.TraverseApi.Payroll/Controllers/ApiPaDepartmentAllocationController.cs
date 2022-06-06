#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.Payroll.Controllers
{
    public class ApiPaDepartmentAllocationController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "allocation/department/{id?}", typeof(AllocDepartmentHeader))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }
        #endregion Web Methods

        #region Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion Override

        protected virtual async Task<EntityList<AllocDepartmentHeader>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.Id, false))))
            {
                if(string.IsNullOrEmpty(id))
                    await Provider.Load<AllocDepartmentHeader>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<AllocDepartmentHeaderBase.Columns>();
                    builder.AppendEquals(AllocDepartmentHeaderBase.Columns.Id, id);
                    var list = new AllocDepartmentHeaderProvider().Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items, FunctionID);
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
            Action<AllocDepartmentHeader> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as AllocDepartmentHeader);
        }
        #endregion Event Handlers

        #region Properties
        protected AllocDepartmentHeaderProvider Provider { get; } = new AllocDepartmentHeaderProvider();

        protected SortedDictionary<string, Action<AllocDepartmentHeader>> PropertyDictionary { get; } = new SortedDictionary<string, Action<AllocDepartmentHeader>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "D30250E2-BE85-41E7-AC07-F8C1014A485F";
        #endregion Fields
    }
}
