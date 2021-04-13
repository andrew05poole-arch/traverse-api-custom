#region Using Directives
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
#endregion Using Directives

namespace TraverseApi
{
    public class ApiDirectRouteProvider : DefaultDirectRouteProvider
    {
        #region Overrides
        protected override IReadOnlyList<IDirectRouteFactory> GetActionRouteFactories(HttpActionDescriptor actionDescriptor)
        {
            return actionDescriptor.GetCustomAttributes<IDirectRouteFactory>(true);
        }
        #endregion Overrides
    }
}