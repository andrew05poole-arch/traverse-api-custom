using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TRAVERSE.Business.API;
using TRAVERSE.Core;

namespace TraverseApi
{
    public sealed class TravApiConfig
    {
        public static string DfltCompanyDB
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("DfltCompanyDB"))
                    return ConfigurationManager.AppSettings["DfltCompanyDB"];

                return null;
            }
        }

        public static string TraverseSysDB
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("TraverseSysDB"))
                    return ConfigurationManager.AppSettings["TraverseSysDB"];

                return null;
            }
        }

        public static string TravLicenseServer
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("TravLicenseServer"))
                    return ConfigurationManager.AppSettings["TravLicenseServer"];

                return null;
            }
        }

        public static int TravLicensePort
        {
            get
            {
                int port = 2113;

                if (ConfigurationManager.AppSettings.AllKeys.Contains("TravLicensePort"))
                    int.TryParse(ConfigurationManager.AppSettings["TravLicensePort"], out port);

                return port;
            }
        }

        public static bool EnableAuditLogProcess
        {
            get
            {
                bool enableAudit = true;

                if (ConfigurationManager.AppSettings.AllKeys.Contains("EnableAuditLogProcess"))
                    bool.TryParse(ConfigurationManager.AppSettings["EnableAuditLogProcess"], out enableAudit);

                return enableAudit;
            }
        }

        public static string ApiTravUsername
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("ApiTravUsername"))
                    return ConfigurationManager.AppSettings["ApiTravUsername"];

                return null;
            }
        }

        public static string ApiTravPassword
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("ApiTravPassword"))
                    return DataSecurity.DecryptData(ConfigurationManager.AppSettings["ApiTravPassword"]);

                return null;
            }
        }

        public static int TraverseCommandTimeout
        {
            get
            {
                int timeout = 30;

                if (ConfigurationManager.AppSettings.AllKeys.Contains("TravCommandTimeout"))
                    int.TryParse(ConfigurationManager.AppSettings["TravCommandTimeout"], out timeout);

                return timeout;
            }
        }

        public static int AuthorizationTimeout
        {
            get => ApiUtility.ApiConfig.AuthorizeTimeout.GetValueOrDefault();
        }

        public static int AccessExpiration
        {
            get => ApiUtility.ApiConfig.AccessExpireHours.GetValueOrDefault();
        }

        public static bool UseSSL
        {
            get => !ApiUtility.ApiConfig.IsDebugLocalEnv.GetValueOrDefault();
        }

        public static string ApiDatabase
        {
            get
            {
                return TraverseSysDB;
            }
        }

        public static string DataProviderFolderPath
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("DataProviderPath"))
                    return ConfigurationManager.AppSettings["DataProviderPath"];

                return ".\\DataProvider";
            }
        }

        public static string TokenEndpoint
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("TokenEndpoint"))
                    return ConfigurationManager.AppSettings["TokenEndpoint"];

                return "/api_token";
            }
        }

        public static string UserPortalTitle
        {
            get
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains("ApiUserPortalTitle"))
                    return ConfigurationManager.AppSettings["ApiUserPortalTitle"];

                return "Traverse API";
            }
        }

        public static List<string> CorsOriginList
        {
            get
            {
                if (_corsOriginList == null)
                {
                    _corsOriginList = new List<string>();
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("CorsOrigins"))
                        _corsOriginList.AddRange(ConfigurationManager.AppSettings["CorsOrigins"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }

                return _corsOriginList;
            }
        }

        private static List<string> _corsOriginList;
    }
}