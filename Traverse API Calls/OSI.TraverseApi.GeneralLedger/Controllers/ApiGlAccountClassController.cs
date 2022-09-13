#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.GeneralLedger;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.GeneralLedger.Controllers
{
    public class ApiGlAccountClassController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "accountclass/{id?}", typeof(AccountClass))]
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

        protected virtual async Task<EntityList<AccountClass>> Load(short? id)
        {
            var builder = new SqlFilterBuilder<AccountClassBase.Columns>();
            if (id.HasValue)
                builder.AppendEquals(AccountClassBase.Columns.AcctClassId, id.ToString());

            var list = await this.Provider.Load<AccountClass>(this.CompId, new FilterCriteria(builder.ToString(), string.Empty), PageNumber, PageSize);

            await this.FilterEntityListAsync(list);

            return list;
        }
        #endregion Helper Methods

        #region Properties
        protected AccountClassProvider Provider { get; } = new AccountClassProvider();
        #endregion Properties

        #region Fields
        private const string FunctionID = "89A94C4E-8378-445B-827E-5F3C747F5417";
        #endregion Fields
    }
}
