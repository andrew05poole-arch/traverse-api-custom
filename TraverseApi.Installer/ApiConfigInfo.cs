#region Using Directives
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
#endregion Using Directives

namespace TraverseApiInstaller
{
    [Serializable]
    public class ApiConfigInfo
    {
        #region Constructors
        private ApiConfigInfo()
        { }
        #endregion Constructors

        #region Private Methods
        private static ApiConfigInfo LoadSettings()
        {
            if (_configuration == null)
            {
                _configuration = new ApiConfigInfo();
                var key = GetRegistryKey(false);

                if (key != null)
                {
                    try
                    {
                        var serial = key.GetValue("Configuration", string.Empty) as string;
                        
                        if (!string.IsNullOrWhiteSpace(serial))
                            _configuration = DeserializeConfiguration(serial);
                    }
                    catch
                    { }
                }
            }
            return _configuration;
        }

        private static string SerializeConfiguration()
        {
            if (Configuration != null)
            {
                try
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    using (MemoryStream objectStream = new MemoryStream())
                    {
                        serializer.Serialize(objectStream, Configuration);
                        objectStream.Position = 0L;

                        using (MemoryStream outputStream = new MemoryStream())
                        {
                            using (GZipStream zipStream = new GZipStream(outputStream,CompressionMode.Compress, true))
                            {
                                objectStream.CopyTo(zipStream);
                            }

                            outputStream.Position = 0L;
                            return Convert.ToBase64String(outputStream.ToArray());
                        }
                    }
                }
                catch
                { }
            }
            return null;
        }

        private static ApiConfigInfo DeserializeConfiguration(string serializedString)
        {
            ApiConfigInfo config = null;

            BinaryFormatter deserializer = new BinaryFormatter();
            if (!string.IsNullOrWhiteSpace(serializedString))
            {
                byte[] serialBytes = Convert.FromBase64String(serializedString);
                using (MemoryStream outputStream = new MemoryStream())
                {
                    using (MemoryStream objectStream = new MemoryStream(serialBytes, false))
                    {
                        objectStream.Position = 0L;
                        using (GZipStream zipStream = new GZipStream(objectStream, CompressionMode.Decompress))
                        {
                            zipStream.CopyTo(outputStream);
                            outputStream.Position = 0L;
                            config = deserializer.Deserialize(outputStream) as ApiConfigInfo;
                        }
                    }
                }
            }

            return config;
        }

        private static RegistryKey GetRegistryKey(bool create)
        {
            var key = Registry.LocalMachine.OpenSubKey("Software\\Open Systems\\Traverse\\API", true);

            if (key == null && create)
                key = Registry.LocalMachine.CreateSubKey("Software\\Open Systems\\Traverse\\API");

            return key;
        }
        #endregion Private Methods

        #region Public Methods
        public static void SaveSettings()
        {
            if (_configuration == null)
                return;

            var key = GetRegistryKey(true);

            if (key == null)
                return; //Do not have permission to edit registry; exit save

            key.SetValue("Configuration", SerializeConfiguration());
        }
        #endregion Public Methods

        #region Properties
        [Bindable(true)]
        public virtual string ServerName { get; set; } = "localhost";

        [Bindable(true)]
        public virtual int ServerPort { get; set; } = 2113;

        [Bindable(true)]
        public virtual string UserName { get; set; }

        [Bindable(true)]
        public virtual string UnencryptedPassword
        {
            get => _password;
            set
            {
                Password = InstallUtility.Helper.Encrypt(_password = value);
            }
        }

        [Bindable(true)]
        public virtual string SystemDatabase { get; set; } = "SYS";

        [Bindable(true)]
        public virtual string DefaultCompany { get; set; }

        [Bindable(true)]
        public virtual string WebsiteName { get; set; } = "TraverseApi";

        [Bindable(true)]
        public virtual string ApplicationName { get; set; }

        [Bindable(true)]
        public virtual string ApiDatabase { get; set; } = "TraverseApi";

        [Bindable(true)]
        public virtual bool OverwriteContent { get; set; }

        public virtual string Password { get; set; }

        public static ApiConfigInfo Configuration
        {
            get => LoadSettings();
        }
        #endregion Properties

        #region Fields
        private string _password = string.Empty;
        private static ApiConfigInfo _configuration;
        #endregion Fields
    }
}
