#region Using Directives
using OSI.TraverseApi.Business;
using System.Collections.Generic;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsReceivable;
using TRAVERSE.Core;
#endregion Using Directives 

namespace TraverseApi.Controllers
{
    public class ApiARTermsCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "termscode/{id?}", typeof(TermsCode))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));

            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "termscode/{id?}", typeof(TermsCode))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<TermsCode> codes = new List<TermsCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var distCode = UpdateTermsCode(item, id);
                if (!codes.Contains(distCode))
                    codes.Add(distCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "termscode", typeof(TermsCode))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<TermsCode> codes = new List<TermsCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var distCode = CreateTermsCode(item);
                this.Provider.Items.Add(distCode);

                if (!codes.Contains(distCode))
                    codes.Add(distCode);
            }
            this.Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "termscode/{id?}", typeof(TermsCode))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<TermsCode> Load(string id)
        {
            SqlFilterBuilder<TermsCodeBase.Columns> builder = new SqlFilterBuilder<TermsCodeBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(TermsCodeBase.Columns.TermsCode, id);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }

        private TermsCode UpdateTermsCode(dynamic item, string id)
        {
            TermsCode termsCode = Find(item.TermsCode ?? id);
            if (termsCode == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.TermsCode ?? id));

            termsCode.Description = item.Description ?? termsCode.Description;
            termsCode.DiscPct = (decimal?)item.DiscPct ?? termsCode.DiscPct;
            termsCode.DiscDayOfMonth = item.DiscDayOfMonth ?? termsCode.DiscDayOfMonth;
            termsCode.DiscDays = (short?)item.DiscDays ?? termsCode.DiscDays;
            termsCode.DiscMinDays = (short?)item.DiscMinDays ?? termsCode.DiscMinDays;
            termsCode.NetDueDays = (short?)item.NetDueDays ?? termsCode.NetDueDays;

            this.ValidateEntity(termsCode);

            return termsCode;
        }

        private TermsCode CreateTermsCode(dynamic item)
        {
            TermsCode termsCode = Find(item.TermsCode);
            if (termsCode != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.TermsCode));
            else
                termsCode = new TermsCode(this.CompId);

            termsCode.TermsCode = item.TermsCode;
            termsCode.Description = item.Description;
            termsCode.DiscPct = (decimal?)item.DiscPct;
            termsCode.DiscDayOfMonth= item.DiscDayOfMonth;
            termsCode.DiscDays = (short?)item.DiscDays;
            termsCode.DiscMinDays = (short?)item.DiscMinDays;
            termsCode.NetDueDays = (short?)item.NetDueDays;

            this.ValidateEntity(termsCode);

            return termsCode;
        }

        private void MarkToDelete(string id)
        {
            TermsCode distCode = Find(id);
            if (distCode == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(TermsCode entity)
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

        private TermsCode Find(string id)
        {
            var header = Provider.Items.Find(TermsCodeBase.Columns.TermsCode, id);
            if (header == null)
            {
                header = EntityProvider.GetEntity<TermsCode, TermsCodeProvider>(new string[] { id }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private TermsCodeProvider Provider { get; } = new TermsCodeProvider();

        private const string FunctionID = "2B8E575A-9294-4CEA-B452-A9472D821C46";
        #endregion Properties
    }
}
