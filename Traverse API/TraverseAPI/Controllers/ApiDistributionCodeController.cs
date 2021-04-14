#region Using Directives
using OSI.TraverseApi.Business;
using System.Collections.Generic;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsPayable;
using TRAVERSE.Core;
#endregion Using Directives 

namespace TraverseApi.Controllers
{
    public class ApiDistributionCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "distributioncode/{id?}", typeof(DistributionCode))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));

            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "distributioncode/{id?}", typeof(DistributionCode))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<DistributionCode> codes = new List<DistributionCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var distCode = UpdateDistributionCode(item, id);
                if (!codes.Contains(distCode))
                    codes.Add(distCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "distributioncode", typeof(DistributionCode))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<DistributionCode> codes = new List<DistributionCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var distCode = CreateDistributionCode(item);
                this.Provider.Items.Add(distCode);

                if (!codes.Contains(distCode))
                    codes.Add(distCode);
            }
            this.Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "distributioncode/{id?}", typeof(DistributionCode))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<DistributionCode> Load(string id)
        {
            SqlFilterBuilder<DistributionCodeBase.Columns> builder = new SqlFilterBuilder<DistributionCodeBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(DistributionCodeBase.Columns.DistCode, id);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }

        private DistributionCode UpdateDistributionCode(dynamic item, string id)
        {
            DistributionCode distCode = Find(item.DistCode ?? id);
            if (distCode == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.DistCode ?? id));

            distCode.Description = item.Description ?? distCode.Description;
            distCode.PayablesGLAcct = item.PayablesGLAcct ?? distCode.PayablesGLAcct;
            distCode.FreightGLAcct = item.FreightGLAcct ?? distCode.FreightGLAcct;
            distCode.MiscGLAcct = item.MiscGLAcct ?? distCode.MiscGLAcct;
            distCode.AccrualGLAcct = item.AccrualGLAcct ?? distCode.AccrualGLAcct;
            distCode.DepositGLAcct = item.DepositGLAcct ?? distCode.DepositGLAcct;

            this.ValidateEntity(distCode);

            return distCode;
        }

        private DistributionCode CreateDistributionCode(dynamic item)
        {
            DistributionCode distCode = Find(item.DistCode);
            if (distCode != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.DistCode));
            else
                distCode = new DistributionCode(this.CompId);

            distCode.DistCode = item.DistCode;
            distCode.Description = item.Description;
            distCode.PayablesGLAcct = item.PayablesGLAcct;
            distCode.FreightGLAcct = item.FreightGLAcct;
            distCode.MiscGLAcct = item.MiscGLAcct;
            distCode.AccrualGLAcct = item.AccrualGLAcct;
            distCode.DepositGLAcct = item.DepositGLAcct;

            this.ValidateEntity(distCode);

            return distCode;
        }

        private void MarkToDelete(string id)
        {
            DistributionCode distCode = Find(id);
            if (distCode == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(DistributionCode entity)
        {
            if (!entity.ValidateAll(true))
            {
                if (entity.BrokenRulesList.Count > 0)
                {
                    throw new InvalidValueException(string.Format("The value for property {0} is not valid. Detail: {1}",
                         entity.BrokenRulesList[0].Property, entity.BrokenRulesList[0].Description));
                }
            }
        }

        private DistributionCode Find(string id)
        {
            var header = Provider.Items.Find(DistributionCodeBase.Columns.DistCode, id);
            if (header == null)
            {
                header = EntityProvider.GetEntity<DistributionCode, DistributionCodeProvider>(new string[] { id }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private DistributionCodeProvider Provider { get; } = new DistributionCodeProvider();

        private const string FunctionID = "F739011B-53E6-4687-9A1C-A623BBEF4066";
        #endregion Properties
    }
}
