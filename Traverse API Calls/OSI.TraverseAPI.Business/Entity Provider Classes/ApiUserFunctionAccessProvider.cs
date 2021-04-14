#region Using Directives
using TRAVERSE.Business;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    internal partial class ApiUserFunctionAccessProvider
    {
        #region Constructors
        private ApiUserFunctionAccessProvider()
        { }
        #endregion Constructors

        #region Static Methods
        internal static ApiUserFunctionAccess GetLastAccess(ApiUserFunction function)
        {
            if (function == null)
                return null;

            SqlFilterBuilder<ApiUserFunctionAccessBase.Columns> builder = new SqlFilterBuilder<ApiUserFunctionAccessBase.Columns>();
            ApiUserFunctionAccessProvider provider = new ApiUserFunctionAccessProvider();

            builder.AppendEquals(ApiUserFunctionAccessBase.Columns.UserFunctionId, function.Id.ToString());
            provider.Load(function.CompId, new FilterCriteria(builder.ToString(), ""));
            if (provider.Items.Count > 0)
                return provider.Items[0];

            return null;
        }

        internal static void UpdateLastAccess(ApiUserFunctionAccess access)
        {
            ApiUserFunctionAccessProvider provider = new ApiUserFunctionAccessProvider();
            provider.Items.Add(access);
            provider.Update(access.CompId);
        }

        internal static EntityList<ApiUserFunctionAccess> LoadAllAccess(string compId)
        {
            var provider = new ApiUserFunctionAccessProvider() { CompId = compId };
            SQLSortBuilder builder = new SQLSortBuilder();
            builder.Append(ApiUserFunctionAccessBase.Columns.UserFunctionId, SQLSortType.ASC);

            return provider.Load(compId, new FilterCriteria("", builder.ToString()));
        }
        #endregion Static Methods
    }
}
