using System;
using System.IO;
using System.Web;
using TRAVERSE.Core;
using TRAVERSE.Web.API.Properties;

namespace TRAVERSE.Web.API
{
    public sealed class TravConnectionManager
    {
        #region Constructors
        /// <summary>
        /// Private constructor to hide the connection manager to a single instance per application
        /// </summary>
        private TravConnectionManager()
        { }
        #endregion Constructors

        #region Methods
        /// <summary>
        /// Initialize Traverse connection information
        /// </summary>
        private void LoadTraverseSettings()
        {
            // Set database information
            ApplicationContext.CompId = TravApiConfig.DfltCompanyDB;
            ApplicationContext.SysDb = TravApiConfig.TraverseSysDB;

            //Set license server information
            ApplicationContext.LicenseServer = TravApiConfig.TravLicenseServer;
            ApplicationContext.LicensePort = TravApiConfig.TravLicensePort;

            //Set path to DataProvider folder
            if (Path.IsPathRooted(TravApiConfig.DataProviderFolderPath))
                ApplicationContext.DataAssemblyPath = TravApiConfig.DataProviderFolderPath;
            else
                ApplicationContext.DataAssemblyPath = ApplicationContext.ResolveRelativePath(TravApiConfig.DataProviderFolderPath);

            //Set whether or not to enable auditing of Traverse transactions performed by the API
            ApplicationContext.EnableAuditor = TravApiConfig.EnableAuditLogProcess;

            //Set the command timeout; Will default to 30 seconds like the client does
            ApplicationContext.CommandTimeout = TravApiConfig.TraverseCommandTimeout;

            //Set sliding cache timeout to be one minute, lowest setting
            ApplicationContext.AddUserSetting<int>("CacheExpiration", 1);
        }

        /// <summary>
        /// Connect to Traverse if not already connected
        /// </summary>
        /// <returns>True if connection is successful; False if connection fails</returns>
        private bool CheckTraverseConnection()
        {
            try
            {
                if (!Connected)
                {
                    ApplicationContext.RootPath = Path.Combine(HttpRuntime.BinDirectory, Resources.ApiClientFolder);
                    
                    string username = TravApiConfig.ApiTravUsername;
                    string password = TravApiConfig.ApiTravPassword;

                    ApplicationContext.ConfigFileName = Path.Combine(HttpRuntime.AppDomainAppPath, "web.config");

                    Connected = WebEnvironment.Setup(username, password, new LoadAppSettings(LoadTraverseSettings));
                    TRAVERSE.Business.CloudUtility.LoadSaaSSetting();
                }
            }
            catch (Exception ex)
            {
                System.Threading.Tasks.Task.FromResult(ApiErrorHandler.ProcessError(ex));
                Connected = false;
            }

            return Connected;
        }
        #endregion Methods

        #region Static Methods
        /// <summary>
        /// Establish connection to Traverse
        /// </summary>
        /// <returns>True when connection is successful. False when connection fails</returns>
        public static bool Connect()
        {
            return ConnectionManager.CheckTraverseConnection();
        }

        /// <summary>
        /// Disconnect from Traverse
        /// </summary>
        public static void Disconnect()
        {
            try
            {
                WebEnvironment.Logout();
            }
            catch
            { }
            ConnectionManager.Connected = false;
        }

        private static TravConnectionManager ConnectionManager { get; } = new TravConnectionManager();
        #endregion Static Methods

        #region Properties
        private bool Connected { get; set; }
        #endregion Properties
    }
}