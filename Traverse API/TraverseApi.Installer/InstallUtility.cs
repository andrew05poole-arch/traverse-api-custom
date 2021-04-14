#region Using Directives
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
#endregion Using Directives

namespace TraverseApiInstaller
{
    public sealed class InstallUtility
    {
        #region Fields
        private Dictionary<string, List<string>> _websiteInfoList;
        public delegate void Status(string message, double percentageComplete);
        public const double NoPctComplete = -1;

        private readonly byte[] ApiKey = new byte[] { 198, 167, 93, 121, 252, 215, 8, 135, 4, 81, 227, 56, 117, 39, 72, 249, 200, 244, 77, 204, 183, 183, 96, 129, 221, 219, 73, 149, 33, 116, 87, 239 };
        private readonly byte[] ApiIV = new byte[] { 166, 232, 13, 70, 31, 4, 105, 40, 194, 158, 11, 113, 23, 134, 172, 77 };
        #endregion Fields

        #region Constructors
        private InstallUtility()
        { }
        #endregion Constructors

        #region Private Methods
        private Dictionary<string, List<string>> LoadWebsiteInfoList()
        {
            if (_websiteInfoList == null)
            {
                _websiteInfoList = new Dictionary<string, List<string>>();
                foreach (var site in WebServerInfo.Sites.OrderBy(s => s.Name))
                {
                    List<string> appList = new List<string>();
                    foreach (var app in site.Applications.OrderBy(a => a.Path.Equals("/") ? a.Path : a.Path.Substring(a.Path.IndexOf('/') + 1)))
                    {
                        appList.Add(app.Path.Substring(app.Path.IndexOf('/') + 1));
                    }
                    _websiteInfoList.Add(site.Name, appList);
                }
            }
            return _websiteInfoList;
        }

        private Site GetSite(string siteName)
        {
            return CurrentSite = WebServerInfo.Sites.FirstOrDefault(s => s.Name == siteName);
        }

        private Application GetApplication(string applicationName)
        {
            if (CurrentSite == null)
                return null;

            return CurrentApplication = CurrentSite.Applications.FirstOrDefault(a => a.Path == string.Format("/{0}", applicationName));
        }
        #endregion Private Methods

        #region Public Methods
        public void ResetSiteInfo(string siteName, string applicationName)
        {
            try
            {
                GetSite(siteName);
                GetApplication(applicationName);
                IsValid = true;
            }
            catch
            {
                IsValid = false;
            }
        }

        public bool StopApplicationPool()
        {
            try
            {
                var appPool = WebServerInfo.ApplicationPools[CurrentApplication.ApplicationPoolName];
                if (appPool.State != ObjectState.Stopped)
                {
                    appPool.Stop();
                    DateTime time = DateTime.Now;
                    bool finished = false;
                    do
                    {
                        finished = (appPool.State == ObjectState.Stopped);
                    }
                    while (!finished || DateTime.Now.Subtract(time).TotalSeconds > 5);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool StopSite()
        {
            try
            {
                if (CurrentSite.State != ObjectState.Stopped)
                {
                    CurrentSite.Stop();
                    DateTime time = DateTime.Now;
                    bool finished = false;
                    do
                    {
                        finished = (CurrentSite.State == ObjectState.Stopped);
                    }
                    while (!finished || DateTime.Now.Subtract(time).TotalSeconds > 5);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool StartApplicationPool()
        {
            try
            {
                var appPool = WebServerInfo.ApplicationPools[CurrentApplication.ApplicationPoolName];
                if (appPool.State != ObjectState.Started)
                {
                    appPool.Start();
                    DateTime time = DateTime.Now;
                    bool finished = false;
                    do
                    {
                        finished = (appPool.State == ObjectState.Started);
                    }
                    while (!finished || DateTime.Now.Subtract(time).TotalSeconds > 5);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool StartSite()
        {
            try
            {
                if (CurrentSite.State != ObjectState.Started)
                {
                    CurrentSite.Start();
                    DateTime time = DateTime.Now;
                    bool finished = false;
                    do
                    {
                        finished = (CurrentSite.State == ObjectState.Started);
                    }
                    while (!finished || DateTime.Now.Subtract(time).TotalSeconds > 5);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string GetApplicationPath()
        {
            if (IsValid)
                return CurrentApplication.VirtualDirectories.FirstOrDefault().PhysicalPath;

            return null;
        }

        public string Encrypt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string processText = text;
            var provider = new AesCryptoServiceProvider() { Padding = PaddingMode.PKCS7 };
            var encryptor = provider.CreateEncryptor(ApiKey, ApiIV);

            byte[] textBytes = UnicodeEncoding.Unicode.GetBytes(processText);
            byte[] codeBytes = encryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);
            processText = Convert.ToBase64String(codeBytes);

            return processText;
        }

        public string Decrypt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string processText = text;
            var provider = new AesCryptoServiceProvider() { Padding = PaddingMode.PKCS7 };
            var decryptor = provider.CreateDecryptor(ApiKey, ApiIV);

            byte[] textBytes = Convert.FromBase64String(processText);
            byte[] codeBytes = decryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);
            processText = UnicodeEncoding.Unicode.GetString(codeBytes);

            return processText;
        }
        #endregion Public Methods

        #region Properties
        public static InstallUtility Helper { get; } = new InstallUtility();

        public Dictionary<string, List<string>> WebsiteInfoList { get => LoadWebsiteInfoList(); }

        private ServerManager WebServerInfo { get; } = new ServerManager();

        private bool IsValid { get; set; }

        private Site CurrentSite { get; set; }

        private Application CurrentApplication { get; set; }
        #endregion Properties
    }
}
