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

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaDepartmentController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "department/{id?}", typeof(Department))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Override
        protected override void AddPropertyDelegates() { }
        #endregion Override       
        protected virtual async Task<EntityList<Department>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.Id, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Department>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<DepartmentBase.Columns>();
                    builder.AppendEquals(DepartmentBase.Columns.Id, id);
                    var list = new DepartmentProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
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
            Action<Department> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Department);
        }
        #endregion Event Handlers

        #region Properties
        protected DepartmentProvider Provider { get; } = new DepartmentProvider();

        protected SortedDictionary<string, Action<Department>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Department>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "6D6C7896-7CCD-46B7-92CD-D28D13A9CFFF";
        #endregion Fields
    }
}
