#region Using Directives
using OSI.TraverseApi.Business;
using System.Collections.Generic;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AccountsPayable;
using TRAVERSE.Core;
#endregion Using Directives

namespace TraverseApi
{
    public class ApiPriorityCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "prioritycode/{id?}", typeof(PriorityCode))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));

            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "prioritycode/{id?}", typeof(PriorityCode))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<PriorityCode> headers = new List<PriorityCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var header = UpdatePriorityCode(item, id);
                if (!headers.Contains(header))
                    headers.Add(header);
            }
            Provider.Update(CompId);

            return Ok(headers);
        }

        [ApiRoute(FunctionID, 2f, "prioritycode", typeof(PriorityCode))]
        public IHttpActionResult Post([FromBody] dynamic body)
        {
            List<PriorityCode> PriorityCodeList = new List<PriorityCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var priorityCode = CreatePriorityCode(item);
                Provider.Items.Add(priorityCode);

                if (!PriorityCodeList.Contains(priorityCode))
                    PriorityCodeList.Add(priorityCode);
            }
            Provider.Update(CompId);

            return Ok(PriorityCodeList);
        }

        [ApiRoute(FunctionID, 2f, "prioritycode/{id?}", typeof(PriorityCode))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<PriorityCode> Load(string id)
        {
            SqlFilterBuilder<PriorityCodeBase.Columns> builder = new SqlFilterBuilder<PriorityCodeBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(PriorityCodeBase.Columns.PriorityCode, id);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }

        private PriorityCode UpdatePriorityCode(dynamic item, string id)
        {
            PriorityCode priorityCode = Find(item.PriorityCode ?? id);
            if (priorityCode == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.PriorityCode ?? id));

            priorityCode.Description = item.Description ?? priorityCode.Description;

            this.ValidateEntity(priorityCode);

            return priorityCode;
        }

        private PriorityCode CreatePriorityCode(dynamic item)
        {
            PriorityCode priorityCode = Find(item.PriorityCode);
            if (priorityCode != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.PriorityCode));
            else
                priorityCode = new PriorityCode(this.CompId);

            priorityCode.PriorityCode = item.PriorityCode ?? priorityCode.PriorityCode;
            priorityCode.Description = item.Description;

            this.ValidateEntity(priorityCode);

            return priorityCode;
        }

        private PriorityCode Find(string id)
        {
            var priorityCode = Provider.Items.Find(PriorityCodeBase.Columns.PriorityCode, id);
            if (priorityCode == null)
            {
                priorityCode = EntityProvider.GetEntity<PriorityCode, PriorityCodeProvider>(new string[] { id }, CompId, null);
                if (priorityCode != null)
                    Provider.Items.Add(priorityCode);
            }
            return priorityCode;
        }

        private void ValidateEntity(PriorityCode entity)
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
            PriorityCode priorityCode = Find(id);
            if (priorityCode == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }
        #endregion Helper Methods

        #region Properties
        private PriorityCodeProvider Provider { get; } = new PriorityCodeProvider();
        private const string FunctionID = "709c38e7-4eee-4516-9ff7-9eedafecfc8f";
        #endregion Properties
    }
}