#region Using Directives
using OSI.TraverseApi.Business.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using TRAVERSE.Business;
using TRAVERSE.Business.UserDefault;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public sealed class ApiUtility
    {
        #region Constructors
        private ApiUtility()
        { }
        #endregion Constructors

        #region Methods
        private List<string> GetApiDbList()
        {
            if (_apiDbList == null || CacheManager.Expired(ApiCacheId))
            {
                lock (_lockManager)
                {
                    _apiDbList = new List<string>();
                    string query = string.Format(Resources.RetrieveApiDbList, ApplicationContext.SysDb);
                    using (IDataReader reader = EntityProvider.ExecuteReader(query, DatasourceType.Normal, false, null, null))
                    {
                        while (reader.Read())
                            _apiDbList.Add(reader.GetString(0));
                    }
                }

                //Add ApiDbList to cache so that Clear Preferences will clear it
                CacheManager.RegisterCache(ApiCacheId, 0,
                    new CacheManager.CacheInvalidator(() => _apiDbList = null));

                if (_apiDbList.Count > 0)
                {
                    CurrentApiDb = _apiDbList[0];
                    _apiDbList.Sort();
                }
            }
            return _apiDbList;
        }

        private ApiInfo GetApiInfo()
        {
            if (_apiInfo == null || !_apiInfo.DatabaseName.Equals(CurrentApiDb))
            {
                _apiInfo = null;
                if (!string.IsNullOrWhiteSpace(CurrentApiDb))
                {
                    _apiInfo = EntityProvider.GetEntity<ApiInfo, ApiInfoProvider>(new string[] { "1" }, CurrentApiDb, null);
                    if (_apiInfo == null)
                    {
                        _apiInfo = new ApiInfo();
                        _apiInfo.Id = 1;
                        _apiInfo.SysDb = ApplicationContext.SysDb;
                        _apiInfo.Version = this.GetType().Assembly.GetName().Version.Build.ToString();
                        _apiInfo.AuthorizeTimeout = 15; //System default will be fifteen minutes; user will adjust
                        _apiInfo.AccessExpireHours = 12; //System default will be twelve hours; user will adjust
                        _apiInfo.RefreshExpireDays = 30; //System default will be thirty days for refresh token; user will adjust
                        _apiInfo.IsDebugLocalEnv = false;
                    }
                    _apiInfo.DatabaseName = CurrentApiDb;
                }
            }
            return _apiInfo;
        }

        public static ApiFunctionHeader GetApiFunction(string apiDatabase, Guid functionId)
        {
            //if no function id, return null
            if (functionId == Guid.Empty)
                return null;

            ApiFunctionHeader header = null;

            //try getting the function out of the list
            if (!Helper._functionList.TryGetValue(functionId, out header))
            {
                //if failed to pull value out of list; load the function and add to the list
                Helper._functionList.Add(functionId, header = ApiFunctionHeaderProvider.GetFunction(apiDatabase, functionId));
            }

            //return function if found
            return header;
        }

        public static void ResetApiFunctionList()
        {
            Helper._functionList.Clear();
        }

        public static void SaveApiInfo()
        {
            ApiInfoProvider provider = new ApiInfoProvider();
            provider.Items.Add(_apiInfo);
            provider.Update(CurrentApiDb);
        }

        public static void ResetApiInfo()
        {
            _apiInfo = null;
        }

        public static string GetHashValue(string inputValue)
        {
            HashAlgorithm hash = new SHA256CryptoServiceProvider();
            byte[] valBytes = Encoding.UTF8.GetBytes(inputValue);
            byte[] hashBytes = hash.ComputeHash(valBytes);
            return Convert.ToBase64String(hashBytes);
        }

        public static string GetApiUserDefault(string compId, string appId, string valueId)
        {
            string value = null;
            if (TryGetApiUserDefault(compId, appId, valueId, ref value))
                return value;

            return value;
        }

        public static bool TryGetApiUserDefault(string compId, string appId, string valueId, ref string value)
        {
            EntityList<UserDefaultValue> valueList = UserDefault.GetDefaultValues(compId, appId, valueId, UserDefaultContextType.User, ApplicationContext.CurrentUser);
            if (valueList != null && valueList.Count > 0)
            {
                value = valueList[0].DefaultValue;
                return true;
            }

            return false;
        }

        public static T ConvertToType<T>(object value, T defaultValue)
        {
            return (T)ConvertToType(value, typeof(T), defaultValue);
        }

        public static object ConvertToType(object value, Type toType, object defaultValue)
        {
            if (value != null && !Convert.IsDBNull(value))
            {
                if (value.GetType() == toType) return value;

                if (toType.IsEnum)
                {
                    if (value is string) return Enum.Parse(toType, value as string);
                    return Enum.ToObject(toType, value);
                }

                if (toType == typeof(byte[]) && value is string && !string.IsNullOrWhiteSpace(value as string))
                {
                    try
                    {
                        return Convert.FromBase64String(value as string);
                    }
                    catch (FormatException)
                    {
                        throw new InvalidValueException("The format of the data supplied is not valid.");
                    }
                }

                try
                {
                    var converter = TypeDescriptor.GetConverter(toType);
                    if (converter != null && converter.CanConvertFrom(value.GetType()))
                        return converter.ConvertFrom(value);

                    string representation = value.ToString();
                    if (converter.CanConvertFrom(typeof(string)) && !string.IsNullOrWhiteSpace(representation))
                        return converter.ConvertFromString(representation);

                    return AttemptConversion(value, toType);
                }
                catch
                {
                    try
                    {
                        return defaultValue;
                    }
                    catch { }
                }
            }

            if (toType.IsValueType)
                return Activator.CreateInstance(toType);

            return null;
        }

        private static object AttemptConversion(object value, Type toType)
        {
            if (!toType.IsInterface && toType.IsGenericType)
            {
                Type internalType = toType.GetGenericArguments()[0];
                object internalValue = ConvertToType<object>(value, internalType);
                return Activator.CreateInstance(toType, new object[] { internalValue });
            }

            if (value is string && (toType == typeof(Guid) || toType == typeof(Version)))
            {
                return Activator.CreateInstance(toType, new object[] { value as string });
            }

            return Convert.ChangeType(value, toType);
        }
        #endregion Methods

        #region Properties
        private static ApiUtility Helper { get; } = new ApiUtility();

        private static Dictionary<string, SqlConnection> ApiSqlConnectionList { get; } = new Dictionary<string, SqlConnection>();

        public static List<string> ApiDbList { get => Helper.GetApiDbList(); }

        public static ApiInfo ApiConfig { get => Helper.GetApiInfo(); }

        public static string CurrentApiDb { get; set; }

        public static DateTimeOffset OAuthRequireDate { get; } = new DateTimeOffset(2021, 5, 1, 0, 0, 0, TimeSpan.Zero);

        public const string ApiCustomFieldName = "custom_fields";
        #endregion Properties

        #region Fields
        private static List<string> _apiDbList;
        private static ApiInfo _apiInfo;
        private static object _lockManager = new object();
        private Dictionary<Guid, ApiFunctionHeader> _functionList = new Dictionary<Guid, ApiFunctionHeader>();
        private static readonly Guid ApiCacheId = new Guid("85387988-5223-4540-AB5E-DB187C8654BC");
        #endregion Fields
    }
}
