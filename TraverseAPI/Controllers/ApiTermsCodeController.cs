#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsPayable;
using TRAVERSE.Core;
#endregion Using Directives

namespace TraverseApi.Controllers
{
    public class ApiTermsCodeController : ApiControllerBase
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
            List<TermsCode> termsCodeList = new List<TermsCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var termsCode = UpdateTermsCode(item, id);
                if (!termsCodeList.Contains(termsCode))
                    termsCodeList.Add(termsCode);
            }
            Provider.Update(CompId);

            return Ok(termsCodeList);
        }

        [ApiRoute(FunctionID, 2f, "termscode", typeof(TermsCode))]
        public IHttpActionResult Post([FromBody] dynamic body)
        {
            List<TermsCode> termsCodeList = new List<TermsCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var termsCode = CreateTermsCode(item);
                Provider.Items.Add(termsCode);

                if (!termsCodeList.Contains(termsCode))
                    termsCodeList.Add(termsCode);
            }
            Provider.Update(CompId);

            return Ok(termsCodeList);
        }

        [ApiRoute(FunctionID, 2f, "termscode/{id?}", typeof(TermsCode))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider.Update(CompId);
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
            termsCode.DiscDays = (short?)item.DiscDays ?? termsCode.DiscDays;
            termsCode.DiscMinDays = (short?)item.DiscMinDays ?? termsCode.DiscMinDays;
            termsCode.NetDueDays = (short?)item.NetDueDays ?? termsCode.NetDueDays;
            termsCode.DiscDayOfMonth = item.DiscDayOfMonth ?? termsCode.DiscDayOfMonth;

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

            termsCode.TermsCode = item.TermsCode ?? termsCode.TermsCode;
            termsCode.Description = item.Description ?? termsCode.Description;
            termsCode.DiscPct = (decimal?)item.DiscPct ?? termsCode.DiscPct;
            termsCode.DiscDays = (short?)item.DiscDays ?? termsCode.DiscDays;
            termsCode.DiscMinDays = (short?)item.DiscMinDays ?? termsCode.DiscMinDays;
            termsCode.NetDueDays = (short?)item.NetDueDays ?? termsCode.NetDueDays;
            termsCode.DiscDayOfMonth = item.DiscDayOfMonth ?? termsCode.DiscDayOfMonth;

            this.ValidateEntity(termsCode);

            return termsCode;
        }

        private TermsCode Find(string id)
        {
            var termsCode = Provider.Items.Find(TermsCodeBase.Columns.TermsCode, id);
            if (termsCode == null)
            {
                termsCode = EntityProvider.GetEntity<TermsCode, TermsCodeProvider>(new string[] { id }, CompId, null);
                if (termsCode != null)
                    Provider.Items.Add(termsCode);
            }
            return termsCode;
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
        private void MarkToDelete(string id)
        {
            TermsCode termsCode = Find(id);
            if (termsCode == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }
        #endregion Helper Methods

        #region Properties
        private TermsCodeProvider Provider { get; } = new TermsCodeProvider();
        private const string FunctionID = "9D107A2E-8AB9-4B76-93F4-BD21B70B806A";
        #endregion Properties
    }
}