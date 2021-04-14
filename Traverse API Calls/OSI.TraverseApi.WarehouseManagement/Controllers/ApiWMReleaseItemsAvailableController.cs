#region Using Directives
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMReleaseItemsAvailableController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "release/available/{locationid}", typeof(NewPickGenerate))]
        public async Task<IHttpActionResult> Get(string locationid)
        {
            return Ok(await this.Load(locationid));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates(){  }
        #endregion

        protected virtual async Task<EntityList<NewPickGenerate>> Load(string locId)
        {
            using (ReleaseItems releaseItems = this.CreateNewProcessEngine())
            {
                releaseItems.TransMan = new TransactionManager(this.CompId);
                SQLStringBuilder sQLStringBuilder = new SQLStringBuilder();
                sQLStringBuilder.AppendEquals("LocId", locId);
                releaseItems.LocId = locId;
                EntityList<NewPickGenerate> entityList = releaseItems.LoadNewOrders(sQLStringBuilder.ToString());
                await this.FilterEntityListAsync(entityList);
                return entityList;
            }
        }

        protected virtual ReleaseItems CreateNewProcessEngine()
        {
            return ProcessBase.LoadProcessEngine<ReleaseItems>(this.CompId);            
        }
        #endregion

        #region Fields
        public const string FunctionID = "45614b7c-d007-47a9-9aef-8082bc28a8d6";
        #endregion
    }
}
