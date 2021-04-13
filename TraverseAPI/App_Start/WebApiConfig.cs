#region Using Directives
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
#endregion Using Directives

namespace TraverseApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes(new ApiDirectRouteProvider());

            //Register custom 404 handler
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{*url}",
                defaults: new { controller = "ApiErrors", action = "Handle" }
            );

            config.EnableCors();

            //Configure Json Formatting
            var formatter = config.Formatters.OfType<JsonMediaTypeFormatter>().First();
            var settings = formatter.SerializerSettings;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            settings.Converters.Add(new ApiJsonConverter());
            settings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize;

            var xmlFrmtList = config.Formatters.OfType<XmlMediaTypeFormatter>();
            if (xmlFrmtList?.FirstOrDefault() != null)
            {
                config.Formatters.Insert(config.Formatters.IndexOf(xmlFrmtList.First()), new ApiXmlMediaTypeFormatter());
            }
            else
                config.Formatters.Add(new ApiXmlMediaTypeFormatter());

            config.EnsureInitialized();
        }
    }
}
