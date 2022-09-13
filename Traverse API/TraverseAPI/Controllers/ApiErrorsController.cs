using System.Web.Http;
using System.Web.Http.Description;

namespace TRAVERSE.Web.API
{
    [ApiAuthorize(true)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ApiErrorsController : ApiControllerBase
    {
        [HttpGet, HttpHead, HttpPost, HttpPut, HttpDelete]
        public IHttpActionResult Handle()
        {
            throw new ApiRouteNotFoundException();
        }

        protected override void AddPropertyDelegates()
        { }
    }
}