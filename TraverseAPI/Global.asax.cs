using OSI.TraverseApi.Business;
using System;
using System.Web;
using System.Web.Http;
using System.Web.Security;

namespace TraverseApi
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_End()
        {
            //Be certain to log out of Traverse
            TravConnectionManager.Disconnect();
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            if (Request == null || Request.Cookies == null)
                return;

            HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie != null)
            {
                try
                {
                    FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
                    var user = HttpContext.Current.Session == null ? null : HttpContext.Current.Session["UserInfo"] as ApiUser;
                    if (user == null)
                    {
                        user = ApiUserProvider.GetUser(TravApiConfig.ApiDatabase, ticket.Name);

                        if (HttpContext.Current.Session != null)
                            HttpContext.Current.Session["UserInfo"] = user;
                    }
                    HttpContext.Current.User = user;
                }
                catch
                { }
            }
        }

        internal static HttpConfiguration Configuration { get; } = new HttpConfiguration();

        internal static void ResetApplication()
        {
            //Cause the application pool to reset
            HttpRuntime.UnloadAppDomain();
        }
    }
}
