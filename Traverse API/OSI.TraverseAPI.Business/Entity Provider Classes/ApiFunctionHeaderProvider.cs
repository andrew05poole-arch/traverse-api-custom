#region Using Directives
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public partial class ApiFunctionHeaderProvider
    {
        #region Fields
        private EntityList<ApiFunctionHeader> _functionList;
        private EntityList<ApiUserFunction> _userFunctionList;
        #endregion Fields

        #region Constructors
        public ApiFunctionHeaderProvider()
            : base()
        { }
        #endregion Constructors

        #region Private Methods
        private EntityList<ApiFunctionHeader> LoadFunctionList()
        {
            if (_functionList == null)
            {
                _functionList = (new ApiFunctionHeaderProvider()).Load(this.CompId, new FilterCriteria("", ApiFunctionHeaderBase.Columns.Name.ToString()));
            }
            return _functionList;
        }

        private EntityList<ApiUserFunction> LoadUserFunctionList()
        {
            if (_userFunctionList == null)
            {
                _userFunctionList = (new ApiUserFunctionProvider()).Load(this.CompId, new FilterCriteria("", ApiUserFunctionBase.Columns.FunctionId.ToString()));
            }
            return _userFunctionList;
        }
        #endregion Private Methods

        #region Protected Methods
        protected override void Update(ApiFunctionHeader entity, bool throwException)
        {
            ApiFunctionSchemaProvider.UpdateSchemaList(entity);
            base.Update(entity, throwException);
        }
        #endregion Protected Methods

        #region Static Methods
        public static EntityList<ApiFunctionHeader> GetEntityList()
        {
            ApiFunctionHeaderProvider provider = new ApiFunctionHeaderProvider();
            return provider.Load(ApiUtility.CurrentApiDb);
        }

        public static ApiFunctionHeader GetFunction(ApiUserFunction function)
        {
            if (function == null || !function.FunctionId.HasValue)
                return null;

            return ApiUtility.GetApiFunction(function.CompId, function.FunctionId.Value);
        }

        public static ApiFunctionHeader GetFunction(string dbName, Guid functionId)
        {
            if (functionId == Guid.Empty)
                return null;

            ApiFunctionHeaderProvider provider = new ApiFunctionHeaderProvider();
            SqlFilterBuilder<ApiFunctionHeaderBase.Columns> builder = new SqlFilterBuilder<ApiFunctionHeaderBase.Columns>(false);
            builder.AppendEquals(ApiFunctionHeaderBase.Columns.Id, functionId.ToString());
            builder.AppendEquals(ApiFunctionHeaderBase.Columns.OverrideId, functionId.ToString());

            SQLSortBuilder sorter = new SQLSortBuilder();
            sorter.Append(ApiFunctionHeaderBase.Columns.OverrideId, SQLSortType.ASC);
            sorter.Append(ApiFunctionHeaderBase.Columns.Id, SQLSortType.DESC);

            provider.Load(dbName, new FilterCriteria(builder.ToString(), sorter.ToString()));
            if (provider.Count > 0)
                return provider[0];

            return null;
        }

        public async static Task<List<ApiFunctionInfo>> GetFunctionInfo(string compId, int? minCount)
        {
            var list = new List<ApiFunctionInfo>();

            await Task.Run(() =>
            {
                var provider = new ApiFunctionHeaderProvider() { CompId = compId };
                var accessList = ApiUserFunctionAccessProvider.LoadAllAccess(compId);

                foreach (var function in provider.FunctionList)
                {
                    var info = new ApiFunctionInfo();

                    info.function_name = function.Name;

                    foreach (var userFunc in provider.UserFunctionList.FindAll(ApiUserFunctionBase.Columns.FunctionId, function.Id))
                    {
                        foreach (var access in accessList.FindAll(ApiUserFunctionAccessBase.Columns.UserFunctionId, userFunc.Id))
                        {
                            info.num_times_accessed += access.AccessCount.GetValueOrDefault();
                            if (!info.first_access_time.HasValue)
                                info.first_access_time = access.FirstAccessTime;
                            else
                                info.first_access_time = access.FirstAccessTime.Value < info.first_access_time.Value ? access.FirstAccessTime : info.first_access_time;

                            if (!info.last_access_time.HasValue)
                            {
                                info.last_access_time = access.LastAccessTime;
                                info.last_access_method = GetMethodTranslation(access.LastAccessMethod.GetValueOrDefault());
                            }
                            else
                                if (access.LastAccessTime.Value > info.last_access_time.Value)
                            {
                                info.last_access_time = access.LastAccessTime;
                                info.last_access_method = GetMethodTranslation(access.LastAccessMethod.GetValueOrDefault());
                            }
                        }
                    }

                    if (minCount.HasValue && info.num_times_accessed < minCount.Value)
                        continue;

                    list.Add(info);
                }
            });

            return list;
        }

        private static string GetMethodTranslation(int methodType)
        {
            switch (methodType)
            {
                case 1:
                    return "get";
                case 2:
                    return "post";
                case 4:
                    return "put";
                case 8:
                    return "delete";
                case 16:
                    return "head";
                case 32:
                    return "schema";
            }
            return string.Empty;
        }
        #endregion Static Methods

        #region Properties
        private EntityList<ApiFunctionHeader> FunctionList { get => LoadFunctionList(); }

        private EntityList<ApiUserFunction> UserFunctionList { get => LoadUserFunctionList(); }
        #endregion Properties
    }
}
