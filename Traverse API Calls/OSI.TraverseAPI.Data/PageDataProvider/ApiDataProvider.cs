using Microsoft.Practices.EnterpriseLibrary.Data;
using OSI.TraverseApi.Business;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using TRAVERSE.Business;
using TRAVERSE.Core;
using TRAVERSE.Data;
using TRAVERSE.Data.Properties;

namespace OSI.TraverseAPI.Data
{
    public sealed class ApiDataProvider : IDataProvider, IApiDataProvider
    {
        #region Constructors
        public ApiDataProvider()
        { }
        #endregion Constructors

        #region Methods
        public void BulkInsert(string compId, DbTransaction transaction, object entities)
        {
            //use existing DataProvider implementation
            DataProvider.BulkInsert(compId, transaction, entities);
        }

        public bool Delete(string compId, IEntity entity, DbTransaction transaction)
        {
            //use existing DataProvider implementation
            return DataProvider.Delete(compId, entity, transaction);
        }

        public DataSet ExecuteCommand(string compId, string commandName, DbTransaction transaction, bool isStoredProcedure, IDictionary<string, object> parameters)
        {
            //use existing DataProvider implementation
            return DataProvider.ExecuteCommand(compId, commandName, transaction, isStoredProcedure, parameters);
        }

        public bool Insert(string compId, IEntity entity, DbTransaction transaction)
        {
            //use existing DataProvider implementation
            return DataProvider.Insert(compId, entity, transaction);
        }

        public IDataReader LoadData(string compId, DbTransaction transaction, FilterCriteria criteria)
        {
            //If page number is less than one (first page), paging isn't active; use the original provider
            //Also, if unable to obtain a thread-safe lock within a full second, use the original provider
            if (PageNumber < 1 || !Monitor.TryEnter(_lockObject, 1000))
                return DataProvider.LoadData(compId, transaction, criteria);

            //Build key column string like [CustId], [ShipToId] from tblArCust
            string keyColumnStmt = "[" + string.Join("], [", KeyColumns) + "]";
            //Build join conditional string like pg.[CustId] = tbl.[CustId] AND pg.[ShipToId] = tbl.[ShipToId]
            string joinString = BuildJoinString();
            //Create order by clause; will sort by key columns if no user specified order
            string orderString = string.IsNullOrEmpty(criteria.OrderBy) ? keyColumnStmt : criteria.OrderBy;
            //Build filter criteria when specified by user
            string whereString = string.IsNullOrEmpty(criteria.WhereClause) ? "" : string.Format(" WHERE {0}", criteria.WhereClause);

            //Generate paging query
            string query = string.Format(Resources.EntityPageQuery, keyColumnStmt, TableName, orderString, joinString, PageSize, PageNumber - 1, orderString.Replace("[", "pg.["));

            //Reset Page Number for any subsequent calls
            PageNumber = 0;
            Monitor.Exit(_lockObject);

            //Create database connection and return results of query
            Database database = ConnectionSetting.GetDatabase(ApplicationContext.DataConnectionString, compId);
            if (transaction == null)
                return database.ExecuteReader(CommandType.Text, query);
            else
                return database.ExecuteReader(transaction, CommandType.Text, query);
        }

        public DataTable LoadLookupData(string compId, string commandString)
        {
            //use existing DataProvider implementation
            return DataProvider.LoadLookupData(compId, commandString);
        }

        public bool Update(string compId, IEntity entity, DbTransaction transaction)
        {
            //use existing DataProvider implementation
            return DataProvider.Update(compId, entity, transaction);
        }

        private string BuildJoinString()
        {
            string joinString = string.Empty;
            foreach (string column in KeyColumns)
            {
                if (joinString.Length != 0)
                    joinString += " AND ";

                joinString += string.Format("pg.[{0}] = tbl.[{0}]", column);
            }

            return joinString;
        }
        #endregion Methods

        #region Properties
        public IDataProvider DataProvider { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; } = 50;

        public string[] KeyColumns { get; set; }

        public string TableName { get; set; }
        #endregion Properties

        #region Fields
        private readonly object _lockObject = new object();
        #endregion Fields
    }
}
