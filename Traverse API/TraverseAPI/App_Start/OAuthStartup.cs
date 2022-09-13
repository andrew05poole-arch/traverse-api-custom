#region Using Directives
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TRAVERSE.Business.API;
using TRAVERSE.Web.API.Properties;
#endregion Using Directives

[assembly: OwinStartup(typeof(TRAVERSE.Web.API.OAuthStartup))]
namespace TRAVERSE.Web.API
{
    public class OAuthStartup
    {
        #region Public Methods
        public void Configuration(IAppBuilder app)
        {
            try
            {
                //Connect to Traverse immediately
                if (!TravConnectionManager.Connect())
                {
                    //if connection fails; kill application so user can reset; error log has been written by this point
                    HttpRuntime.UnloadAppDomain();
                    return;
                }

                //Set Api Database from settings
                ApiUtility.CurrentApiDb = TravApiConfig.ApiDatabase;
                ConfigureOAuth(app);
                LoadPluginAssemblies();

                //Standard launch procedures to load routes
                AreaRegistration.RegisterAllAreas();
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                WebApiConfig.Register(WebApiApplication.Configuration);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);

                //Enable our custom error handling process
                WebApiApplication.Configuration.Services.Replace(typeof(System.Web.Http.ExceptionHandling.IExceptionHandler), ApiErrorHandler.ErrorHandler);

                app.UseWebApi(WebApiApplication.Configuration);
            }
            catch (Exception ex)
            {
                System.Threading.Tasks.Task.FromResult(ApiErrorHandler.ProcessError(ex));
            }
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Configure OAuth and security protocols
        /// </summary>
        /// <param name="app"></param>
        private void ConfigureOAuth(IAppBuilder app)
        {
            OAuthAuthorizationServerOptions OAuthServerOptions = new OAuthAuthorizationServerOptions()
            {
                AllowInsecureHttp = !TravApiConfig.UseSSL,
                TokenEndpointPath = new PathString(TravApiConfig.TokenEndpoint),
                AccessTokenExpireTimeSpan = TimeSpan.FromHours(TravApiConfig.AccessExpiration),
                Provider = new TraverseApiAuthServerProvider(),
                SystemClock = new SystemClock(),
                AuthorizationCodeExpireTimeSpan = TimeSpan.FromMinutes(TravApiConfig.AuthorizationTimeout),
                RefreshTokenProvider = new TraverseApiRefreshTokenProvider()
            };

            // Token Generation
            app.UseOAuthAuthorizationServer(OAuthServerOptions);
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
            app.UseCors(CorsOptions.AllowAll);
        }

        /// <summary>
        /// Load plugin assemblies so that the controllers are added in with the standard route mapping process
        /// </summary>
        private void LoadPluginAssemblies()
        {
            string path = Resources.ApiPluginFolder;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(HttpRuntime.BinDirectory, path);

            if (!Directory.Exists(path))
                return;

            foreach (string file in Directory.GetFiles(path, "*.dll"))
            {
                System.Reflection.Assembly.LoadFile(file);
            }
        }
        #endregion Private Methods
    }
}