using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using TRAVERSE.Business.API;
using TRAVERSE.Web.API.Properties;

namespace TRAVERSE.Web.API
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class ApiSystemController : ApiControllerBase
    {
        [ApiRoute("api_version", true, null)]
        public async Task<IHttpActionResult> GetVersion()
        {
            return await Task.Run(() =>
            {
                List<dynamic> list = new List<dynamic>();
                var dictionary = ApiAssemblyHelper.LoadAssemblyInfo(System.Reflection.Assembly.GetExecutingAssembly());

                dynamic item = new ApiEntityModel();
                ((IDictionary<string, object>)item).Add(Resources.ApiAssemblyVersion, ApiUtility.ApiConfig.Version);
                ((IDictionary<string, object>)item).Add(Resources.ApiAssemblyCount, dictionary.Count);

                list.Add(item);

                foreach (KeyValuePair<string, ApiAssemblyHelper.AssemblyDetailInfo> pair in dictionary.OrderBy(n => n.Key))
                {
                    item = new ApiEntityModel();

                    ((IDictionary<string, object>)item).Add(Resources.ApiVersionTableAssemblyName, pair.Value.AssemblyName);
                    ((IDictionary<string, object>)item).Add(Resources.ApiVersionTableAssemblyVersion, pair.Value.AssemblyVersion);
                    ((IDictionary<string, object>)item).Add(Resources.ApiVersionTableAssemblyDate, pair.Value.AssemblyDate);

                    list.Add(item);
                }

                return Ok<List<dynamic>>(list);
            });
        }

        [ApiRoute("api_func_info", true, typeof(ApiFunctionInfo))]
        [ApiRoute("api_func_info/{min_count:int}", true, typeof(ApiFunctionInfo))]
        public async Task<IHttpActionResult> GetFunctionInfo(int? min_count = null)
        {
            return Ok(await ApiFunctionHeaderProvider.GetFunctionInfo(TravApiConfig.ApiDatabase, min_count));
        }

        [ApiRoute("api_user_info", true, null)]
        public async Task<IHttpActionResult> GetUserInfo()
        {
            return Ok(await ApiUserProvider.GetUserListForWeb(TravApiConfig.ApiDatabase));
        }

        [ApiRoute("api_error_log", true, null)]
        [ApiRoute("api_error_log/{filename?}", true, null)]
        public async Task<IHttpActionResult> GetErrorLog(string filename = null)
        {
            List<ApiErrorLogEntry> list = new List<ApiErrorLogEntry>();
            string dir = Path.Combine(HttpRuntime.AppDomainAppPath, "ErrorLog");
            if (string.IsNullOrWhiteSpace(filename))
                return Ok(await ApiErrorLogInfo.GetLogErrorDirectory(dir));

            try
            {
                await Task.Run(() =>
                {
                    string name = filename.EndsWith(".log") ? filename : string.Format("{0}.log", filename);
                    string path = Path.Combine(dir, name);

                    list = ApiErrorHandler.ReadErrorFile(path);
                });
            }
            catch (FileNotFoundException)
            {
                throw new ApiRequestException("Error file does not exist.");
            }

            return Ok(list);
        }

        protected override void AddPropertyDelegates() { }
    }
}