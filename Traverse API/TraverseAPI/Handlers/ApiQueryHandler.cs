using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Business.API;
using TRAVERSE.Web.API.Properties;

namespace TRAVERSE.Web.API
{
    internal sealed class ApiQueryHandler
    {
        #region Constructors
        private ApiQueryHandler()
        { }
        #endregion Constructors

        #region Methods
        public static async Task<List<object>> LoadQueryData(ApiUser user, string companyId, string query)
        {
            ApiQueryHandler handler = new ApiQueryHandler() { User = user, CompanyId = companyId, Query = query };
            return await handler.Process();
        }

        private async Task<List<object>> Process()
        {
            await ParseQuery();
            return await ProcessData();
        }

        private async Task ParseQuery()
        {
            if (string.IsNullOrEmpty(Query))
                throw new ApiRequestException(Resources.ApiSelectBlank);

            try
            {
                await ReplaceTokens();
            }
            catch (Exception ex)
            {
                if (ex is PermissionDeniedException)
                    throw ex;

                throw new ApiRequestException(Resources.ApiInvalidSelectStmt, ex);
            }
        }

        private async Task ReplaceTokens()
        {
            string query = Query;
            int index = 0;
            do
            {
                index = query.IndexOf("$[");
                if (index >= 0)
                {
                    int end = query.IndexOf("]$", index);

                    string functionName = end > 0 ? query.Substring(index + 2, end - index - 2) : query.Substring(index + 2);
                    string alias = string.Empty;
                    int aliasPipe = functionName.IndexOf('|');
                    if (aliasPipe > 0)
                    {
                        alias = string.Format(" as [{0}] ", functionName.Substring(aliasPipe + 1).Replace("[", "").Replace("]", ""));
                        functionName = functionName.Substring(0, aliasPipe);
                    }
                    else
                        alias = string.Format(" as [{0}]", functionName.Replace(" ", "_"));

                    string left = query.Substring(0, index);
                    string right = end > 0 ? query.Substring(end + 2) : string.Empty;

                    query = string.Format("{0}{1}{2}{3}", left, await GetFunctionSubQuery(functionName.Replace("[", "").Replace("]", "")), alias, right);
                }
            }
            while (index >= 0);

            GeneratedSql = query;
        }

        private async Task<string> GetFunctionSubQuery(string functionName)
        {
            return
            await Task.Run(() =>
            {
                if ((User.ExpirationDate.GetValueOrDefault(DateTime.Now) - DateTime.Now).Days >= 0)
                {
                    var function = User.FunctionList.Find(f => f.FunctionInfo != null && f.FunctionName == functionName && (f.AccessExpireDate.GetValueOrDefault(DateTime.Now) - DateTime.Now).Days >= 0);
                    if (function != null)
                    {
                        var access = function.CompanyList.Find(c => c.CompanyId.ToLower() == CompanyId);
                        return access.BuildSqlTableCommand();
                    }
                }
                throw new PermissionDeniedException(string.Format(Resources.ApiInvalidFunction, functionName));
            });
        }

        private async Task<List<dynamic>> ProcessData()
        {
            List<dynamic> list = new List<dynamic>();
            await Task.Run(() =>
            {
                List<string> columnList = new List<string>();
                var data = EntityProvider.ExecuteCommand(GeneratedSql, CompanyId, null);
                if (data != null && data.Tables.Count == 1)
                {
                    foreach (DataColumn column in data.Tables[0].Columns)
                        columnList.Add(column.ColumnName);

                    foreach (DataRow row in data.Tables[0].Rows)
                    {
                        dynamic item = new ApiEntityModel();
                        foreach (string col in columnList)
                        {
                            ((IDictionary<string, object>)item).Add(col, Convert.IsDBNull(row[col]) ? null : row[col]);
                        }
                        list.Add(item);
                    }
                }
            });

            return list;
        }
        #endregion Methods

        #region Properties
        public ApiUser User { get; private set; }

        public string CompanyId { get; private set; }

        public string Query { get; private set; }

        private string GeneratedSql { get; set; } = string.Empty;
        #endregion Properties
    }
}