#region Using Directives
using OSI.TraverseApi.Business.Properties;
using System;
using System.Data;
using System.Data.SqlClient;
using TRAVERSE.Business;
using TRAVERSE.Core;
#endregion Using Directives

namespace OSI.TraverseApi.Business
{
    public sealed class ApiCreateDb : ProcessBase
    {
        #region Fields
        PropertyCollection<string> _invalidPropertyList;
        #endregion Fields

        #region Constructors
        public ApiCreateDb()
            : this(string.Empty)
        { }

        public ApiCreateDb(string compId)
            : this(compId, ProcessBase.GenerateProcessId())
        { }

        public ApiCreateDb(string compId, string processId)
            : base(compId, processId)
        { }
        #endregion Constructors

        #region Private
        private void RebuildSqlConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ApplicationContext.DataConnectionString);
            MasterUsername = builder.UserID;

            if (UseTrusted)
                builder.IntegratedSecurity = UseTrusted;
            else
            {
                builder.UserID = Username;
                builder.Password = Password;
            }

            builder.PersistSecurityInfo = true;
            builder.InitialCatalog = "master";

            Connection = new SqlConnection(builder.ToString());
        }

        private void CreateDatabase()
        {
            SqlCommand command = new SqlCommand();
            command.Connection = Connection;
            command.CommandType = CommandType.Text;
            command.CommandText = string.Format(Resources.ApiCreateDatabase, DatabaseName);
            command.ExecuteNonQuery();
        }

        private void CreateInfoTable()
        {
            SqlCommand command = new SqlCommand();
            command.Connection = Connection;
            command.CommandType = CommandType.Text;
            command.CommandText = string.Format(Resources.ApiCreateInfoTable, DatabaseName, SqlUtil.Encode(ApplicationContext.SysDb, true));
            command.ExecuteNonQuery();
        }

        private void ApplyMasterLogin()
        {
            SqlCommand command = new SqlCommand();
            command.Connection = Connection;
            command.CommandType = CommandType.Text;
            command.CommandText = string.Format(Resources.ApiAddTravMasterUser, DatabaseName, MasterUsername);
            command.ExecuteNonQuery();
        }

        private bool DatabaseExists()
        {
            bool exists = false;
            string query = string.Format(Resources.ApiDatabaseExits, SqlUtil.Encode(this.DatabaseName, true));
            var ds = EntityProvider.ExecuteCommand(query, ApplicationContext.SysDb, null);
            if(ds != null && ds.Tables.Count>0 
                && ds.Tables[0].Rows.Count>0 
                && ds.Tables[0].Rows[0]["DBExists"] != DBNull.Value)
            {
                exists = true;
            }
            return exists;
        }
        #endregion Private

        #region Public
        public bool ValidateProperties()
        {
            //clear the invalid property list 
            this.InvalidPropertyList.Clear();

            //validate the properties
            if (string.IsNullOrWhiteSpace(this.DatabaseName))
            {
                this.InvalidPropertyList.Add(new EntityProperty<string>("DatabaseName", "Api Database Name is required."));
            }

            if (ApplicationContext.IsSaaS
              && !string.IsNullOrWhiteSpace(this.DatabaseName)
              && this.DatabaseName.Length <= 3)
            {
                this.InvalidPropertyList.Add(new EntityProperty<string>("DatabaseName", "Api Database Name must be greater than 3 characters."));
            }

            if (this.InvalidPropertyList.Count == 0
                && this.DatabaseExists())
            {
                this.InvalidPropertyList.Add(new EntityProperty<string>("DatabaseName", "Api Database already exists."));
            }

            return (this.InvalidPropertyList.Count == 0);
        }
        #endregion

        #region Overrides
        public override void Execute(Status status)
        {
            this.ProcessStatus = status;

            //validate required property values
            if (ValidateProperties() == false)
            {
                throw new InvalidValueException("One or more property values is invalid.  Refer to the InvalidPropertyList for details.");
            }

            try
            {            
              
                this.RaiseStatus("Preparing");
                this.RebuildSqlConnectionString();
                this.RaiseStatus("Creating database");
                Connection.Open();
                CreateDatabase();
                this.RaiseStatus("Create tables");
                CreateInfoTable();
                this.RaiseStatus("Apply master user login");
                this.ApplyMasterLogin();
                this.RaiseStatus("Finishing up");
                ApiUtility.CurrentApiDb = DatabaseName;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection != null)
                {
                    if (Connection.State == ConnectionState.Open)
                        Connection.Close();

                    Connection.Dispose();
                    Connection = null;
                }
            }
        }
        #endregion Overrides

        #region Properties

        /// <summary>
        /// Gets the list of invalid properties
        /// </summary>
        public  PropertyCollection<string> InvalidPropertyList
        {
            get
            {
                if (_invalidPropertyList == null)
                    _invalidPropertyList = new PropertyCollection<string>();
                return _invalidPropertyList;
            }
        }

        public string DatabaseName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool UseTrusted { get; set; }

        private string MasterUsername { get; set; }

        private SqlConnection Connection { get; set; }
        #endregion Properties
    }
}
