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
        #endregion Private

        #region Overrides
        public override void Execute(Status status)
        {
            try
            {
                this.ProcessStatus = status;
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
        public string DatabaseName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool UseTrusted { get; set; }

        private string MasterUsername { get; set; }

        private SqlConnection Connection { get; set; }
        #endregion Properties
    }
}
