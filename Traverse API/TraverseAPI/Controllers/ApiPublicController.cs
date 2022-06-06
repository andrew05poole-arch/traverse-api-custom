using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using TRAVERSE.Business.API;
using TraverseApi.Properties;

namespace TraverseApi
{
    //These routes require a user to be authorized
    public class ApiPublicController : ApiControllerBase
    {
        [ApiRoute("function_company", false, null)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> GetCompanyList()
        {
            List<dynamic> list = new List<dynamic>();
            await Task.Run(() =>
            {
                var user = Request.GetOwinContext().Get<ApiUser>(Resources.ApiUserInfoStorage);
                if (user != null)
                {
                    user.FunctionList.ForEach(f =>
                    {
                        dynamic model = new ExpandoObject();
                        model.function_name = f.FunctionName;
                        List<dynamic> companyList = new List<dynamic>();
                        foreach (var company in f.CompanyList)
                        {
                            dynamic compModel = new ExpandoObject();
                            compModel.Company_id = company.CompanyId;
                            compModel.get = company.AllowRead;
                            compModel.post = company.AllowNew;
                            compModel.put = company.AllowEdit;
                            compModel.delete = company.AllowDelete;
                            companyList.Add(compModel);
                        }
                        model.company_list = companyList;
                        list.Add(model);
                    });
                }
            });
            return new ApiOkResult(ApplyPaging(list), this);
        }

        [ApiRoute("data", false, null)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> GetQueryData()
        {
            string companyId = string.Empty;
            string query = string.Empty;

            foreach (var pair in Request.GetQueryNameValuePairs())
            {
                if (pair.Key.ToLower() == Resources.ApiRequestCompany)
                    companyId = pair.Value;
                if (pair.Key.ToLower() == Resources.ApiRequestQuery)
                    query = pair.Value;
            }

            if (string.IsNullOrWhiteSpace(query))
                query = await Request.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(companyId))
                throw new ApiRequestException(Resources.ApiCompanyRequired);

            if (string.IsNullOrEmpty(query))
                throw new ApiRequestException(Resources.ApiSelectBlank);

            var user = Request.GetOwinContext().Get<ApiUser>(Resources.ApiUserInfoStorage);
            var response = await ApiQueryHandler.LoadQueryData(user, companyId.ToLower(), query);
            return new ApiOkResult(ApplyPaging(response), this);
        }

        protected override void AddPropertyDelegates() { }

        private KeyValuePair<int, List<dynamic>> ApplyPaging(List<dynamic> list)
        {
            int totalCount = 0;
            var pageList = new List<dynamic>();

            if (PageNumber > 0 && PageSize > 0)
            {
                int count = PageSize;
                int start = (PageNumber - 1) * PageSize;
                foreach (object item in list)
                {
                    if (totalCount >= start && totalCount < (start + count))
                        pageList.Add(item);

                    if (totalCount >= (start + count))
                        break;

                    totalCount++;
                }
            }
            else
                pageList.AddRange(list);

            return new KeyValuePair<int, List<dynamic>>(list.Count, pageList);
        }
    }

    //These routes are completely public and do not require a user to be authorized
    [ApiAuthorize(true)]
    public class ApiTimeController : ApiControllerBase
    {
        [ApiRoute("api_time", false, typeof(TimeResponse))]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult GetTime()
        {
            TimeResponse response = new TimeResponse();
            response.current_system_date = DateTime.Now;
            return Ok(response);
        }

        protected override void AddPropertyDelegates() { }
    }

    internal class TimeResponse
    {
        public DateTime current_system_date { get; set; }
    }
}
