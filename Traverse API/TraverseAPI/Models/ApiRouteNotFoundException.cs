using System;
using System.Web;

namespace TraverseApi
{
    public sealed class ApiRouteNotFoundException : Exception
    {
        public override string Message => HttpContext.Current != null ?
            string.Format("The specified url [{0}] is invalid", HttpContext.Current.Request.Url.PathAndQuery) :
            "The specified route is invalid";
    }
}