#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#endregion Using Directives

namespace TraverseApiInstaller
{
    using static InstallUtility;

    public sealed class ProcessWebInstall
    {
        #region Constructors
        public ProcessWebInstall()
        { }
        #endregion Constructors

        #region Public Methods
        public string Validate()
        {
            if (ApiConfigInfo.Configuration == null)
                return "Configuration settings cannot be found.";

            if (string.IsNullOrWhiteSpace(ApiConfigInfo.Configuration.ServerName))
                return "Server name is required.";

            if (ApiConfigInfo.Configuration.ServerPort <= 0)
                return "Server port is invalid.";

            if (string.IsNullOrWhiteSpace(ApiConfigInfo.Configuration.UserName))
                return "Username is required.";

            if (string.IsNullOrWhiteSpace(ApiConfigInfo.Configuration.SystemDatabase))
                return "SYS database is required.";

            if (string.IsNullOrWhiteSpace(ApiConfigInfo.Configuration.DefaultCompany))
                return "Default company is required.";

            if (!InstallUtility.Helper.WebsiteInfoList.ContainsKey(ApiConfigInfo.Configuration.WebsiteName))
                return "Invalid website is selected.";

            if (!InstallUtility.Helper.WebsiteInfoList[ApiConfigInfo.Configuration.WebsiteName].Contains(ApiConfigInfo.Configuration.ApplicationName))
                return "Invalid application is selected.";

            if (string.IsNullOrWhiteSpace(ApiConfigInfo.Configuration.ApiDatabase))
                return "A database name for the Api is required.";

            return string.Empty;
        }

        public void Execute(Status status)
        {
            this.ProcessStatus = status;
            Helper.ResetSiteInfo(ApiConfigInfo.Configuration.WebsiteName, ApiConfigInfo.Configuration.ApplicationName);

            RaiseStatus("Stopping services");

            if (!Helper.StopApplicationPool() ||
                !Helper.StopSite())
                throw new Exception("Unable to stop either the application pool or the site");

            RaiseStatus("Preparing install");
            string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apiInstallPath = Environment.ExpandEnvironmentVariables(Helper.GetApplicationPath());
            string webPath = Path.Combine(currentPath, "WebInstall");

            this.RemoveContentFiles(apiInstallPath);
            MoveFiles(webPath, apiInstallPath);
            RaiseStatus("Updating config settings");
            WriteConnectionConfig(apiInstallPath);

            RaiseStatus("Finishing up");
            if (Directory.Exists(webPath))
                ClearDirectory(webPath);
        }
        #endregion Public Methods

        #region Private Methods
        private void RaiseStatus(string message)
        {
            this.RaiseStatus(message, NoPctComplete);
        }

        private void RaiseStatus(string message, double percentageComplete)
        {
            if (this.ProcessStatus != null)
                this.ProcessStatus.Invoke(message, percentageComplete);
        }

        private void RemoveContentFiles(string basePath)
        {
            if (!ApiConfigInfo.Configuration.OverwriteContent)
                return;

            this.ClearDirectory(Path.Combine(basePath, "Content"));
            this.ClearDirectory(Path.Combine(basePath, "Views"));
        }

        private void MoveFiles(string sourcePath, string destPath)
        {
            List<string> fileList = new List<string>(Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories));

            if (!destPath.EndsWith("\\"))
            {
                destPath += "\\";
            }

            double count = 1;
            double numFiles = fileList.Count;
            double percent = 0;
            foreach (string file in fileList)
            {
                if (++count % 5 == 0)
                    percent = count / numFiles;

                RaiseStatus(
                    string.Format("Moving {0}",
                    Path.GetFileNameWithoutExtension(file)), percent);

                string destination = file.Replace(sourcePath, destPath);
                if (Path.GetFileName(file).Equals("ApiConnectionInfo.config", StringComparison.OrdinalIgnoreCase) && File.Exists(destination))
                {
                    File.Delete(file);
                    continue;
                }

                this.CreateDirectory(Path.GetDirectoryName(destination));

                if (File.Exists(destination))
                {
                    this.InheritSecurity(destination);
                    File.SetAttributes(destination, FileAttributes.Normal);
                    File.Delete(destination);
                }

                this.InheritSecurity(file);
                File.Move(file, destination);

                this.InheritSecurity(destination);
            }

            RaiseStatus("Cleaning up");
            //Now let's cleanup our temporary directories
            string[] directoryList = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);
            for (int i = directoryList.Length; i > 0; i--)
            {
                Directory.Delete(directoryList[i - 1]);
            }
            Directory.Delete(sourcePath);
        }

        private void CreateDirectory(string path)
        {
            List<string> dirListNeeded = new List<string>();
            string directory = path;
            do
            {
                if (Directory.Exists(directory))
                {
                    break;
                }
                dirListNeeded.Add(directory);
                directory = Path.GetDirectoryName(directory);
            }
            while (!string.IsNullOrEmpty(directory));

            for (int i = dirListNeeded.Count - 1; i >= 0; i--)
            {
                Directory.CreateDirectory(dirListNeeded[i]);
            }
        }

        private void InheritSecurity(string filePath)
        {
            FileInfo info = new FileInfo(filePath);
            var security = info.GetAccessControl();
            var ruleList = security.GetAccessRules(true, false, typeof(System.Security.Principal.NTAccount));
            security.SetAccessRuleProtection(false, true);
            info.SetAccessControl(security);

            foreach (System.Security.AccessControl.FileSystemAccessRule rule in ruleList)
            {
                if (rule.FileSystemRights != System.Security.AccessControl.FileSystemRights.ReadAndExecute)
                {
                    security.RemoveAccessRuleSpecific(rule);
                }
            }

            info.SetAccessControl(security);
        }

        private void WriteConnectionConfig(string path)
        {
            string filePath = Path.Combine(path, "ApiConnectionInfo.config");
            int startIndex;
            int endIndex;

            string text = File.ReadAllText(filePath);
            startIndex = text.IndexOf("\"TravLicenseServer\"");
            startIndex = text.IndexOf("\"", startIndex + 19);
            endIndex = text.IndexOf("\"", startIndex + 1);
            text = text.Substring(0, startIndex + 1) + (ApiConfigInfo.Configuration.ServerName ?? string.Empty).Trim() + text.Substring(endIndex);

            startIndex = text.IndexOf("\"TravLicensePort\"");
            startIndex = text.IndexOf("\"", startIndex + 17);
            endIndex = text.IndexOf("\"", startIndex + 1);
            text = text.Substring(0, startIndex + 1) + ApiConfigInfo.Configuration.ServerPort.ToString() + text.Substring(endIndex);

            startIndex = text.IndexOf("\"TraverseSysDB\"");
            startIndex = text.IndexOf("\"", startIndex + 15);
            endIndex = text.IndexOf("\"", startIndex + 1);
            text = text.Substring(0, startIndex + 1) + (ApiConfigInfo.Configuration.SystemDatabase ?? string.Empty).Trim() + text.Substring(endIndex);

            startIndex = text.IndexOf("\"DfltCompanyDB\"");
            startIndex = text.IndexOf("\"", startIndex + 15);
            endIndex = text.IndexOf("\"", startIndex + 1);
            text = text.Substring(0, startIndex + 1) + (ApiConfigInfo.Configuration.DefaultCompany ?? string.Empty).Trim() + text.Substring(endIndex);

            startIndex = text.IndexOf("\"ApiTravUsername\"");
            startIndex = text.IndexOf("\"", startIndex + 17);
            endIndex = text.IndexOf("\"", startIndex + 1);
            text = text.Substring(0, startIndex + 1) + (ApiConfigInfo.Configuration.UserName ?? string.Empty).Trim() + text.Substring(endIndex);

            startIndex = text.IndexOf("\"ApiTravPassword\"");
            startIndex = text.IndexOf("\"", startIndex + 17);
            endIndex = text.IndexOf("\"", startIndex + 1);
            text = text.Substring(0, startIndex + 1) + (ApiConfigInfo.Configuration.Password ?? string.Empty) + text.Substring(endIndex);

            startIndex = text.IndexOf("\"ApiDatabase\"");
            startIndex = text.IndexOf("\"", startIndex + 13);
            endIndex = text.IndexOf("\"", startIndex + 1);
            text = text.Substring(0, startIndex + 1) + (ApiConfigInfo.Configuration.ApiDatabase ?? string.Empty).Trim() + text.Substring(endIndex);

            File.WriteAllText(filePath, text);
        }

        private void ClearDirectory(string path)
        {
            string[] dirs = Directory.GetDirectories(path);
            if (dirs != null)
            {
                foreach (string dir in dirs)
                {
                    string[] files = Directory.GetFiles(dir);
                    if (files != null)
                    {
                        foreach (string file in files)
                            File.Delete(file);
                    }
                    Directory.Delete(dir);
                }
            }
        }
        #endregion Private Methods

        #region Properties
        private Status ProcessStatus { get; set; }
        #endregion Properties
    }
}
