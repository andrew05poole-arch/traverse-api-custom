using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRAVERSE.Business;
using TRAVERSE.Business.GeneralLedger;
using TRAVERSE.Core;

namespace OSI.TraverseApi.GeneralLedger
{
    public class ApiTransactionHeaderProvider : TransactionProviderBase<TransactionHeader>
    {
        #region Constructors
        public ApiTransactionHeaderProvider()
        { }
        #endregion Constructors

        #region Methods
        protected override void LoadData(string compId, FilterCriteria criteria)
        {
            var builder = new SqlFilterBuilder<TransactionBase.Columns>();
            builder.AppendIsNull(TransactionBase.Columns.AllocParentId);
            
            string filter = builder.ToString();
            
            if (!string.IsNullOrWhiteSpace((criteria?.WhereClause)))
                    filter = string.Concat(filter, " AND (", criteria.WhereClause, ")");

            base.LoadData(compId, new FilterCriteria(filter, criteria?.OrderBy ?? ""));
        }

        public override void Update(string compId)
        {
            var provider = new TransactionHeaderProvider();
            provider.Items.AddRange(this.Items.ChangedItems);
            provider.Items.DeletedItems.AddRange(this.Items.DeletedItems);
            provider.Update(compId);

            if (!provider.Items.IsDirty)
                this.Items.DeletedItems.Clear();
        }
        #endregion Methods
    }
}
