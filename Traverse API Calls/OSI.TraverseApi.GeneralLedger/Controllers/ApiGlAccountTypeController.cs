#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.GeneralLedger;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.GeneralLedger.Controllers
{
    public class ApiGlAccountTypeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "accounttype/{id?}", typeof(AccountType))]
        public async Task<IHttpActionResult> Get(short? id = null)
        {
            return Ok(await this.Load(id));
        }
        #endregion Web Mehods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            //read-only class; no properties to edit
        }

        protected virtual async Task<EntityList<AccountType>> Load(short? id)
        {
            var builder = new SqlFilterBuilder<AccountTypeBase.Columns>();
            if (id.HasValue)
                builder.AppendEquals(AccountTypeBase.Columns.AcctTypeId, id.ToString());

            var list = await this.Provider.Load<AccountType>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);

            await this.FilterEntityListAsync(list);
            return list;
        }
        #endregion Helper Methods

        #region Properties
        protected AccountTypeProvider Provider { get; } = new AccountTypeProvider();
        #endregion Properties

        #region Fields
        private const string FunctionID = "C5523178-ABA9-4B6D-B587-79ADB07195A8";
        #endregion Fields
    }
}
