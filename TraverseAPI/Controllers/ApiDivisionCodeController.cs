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
    public class ApiDivisionCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "divisioncode/{id?}", typeof(DivisionCode))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));

            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "divisioncode/{id?}", typeof(DivisionCode))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<DivisionCode> codes = new List<DivisionCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var divisionCode = UpdateDivisionCode(item, id);
                if (!codes.Contains(divisionCode))
                    codes.Add(divisionCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "divisioncode", typeof(DivisionCode))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<DivisionCode> codes = new List<DivisionCode>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var divisionCode = CreateDivisionCode(item);
                Provider.Items.Add(divisionCode);

                if (!codes.Contains(divisionCode))
                    codes.Add(divisionCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "divisioncode/{id?}", typeof(DivisionCode))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);     
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<DivisionCode> Load(string id)
        {
            SqlFilterBuilder<DivisionCodeBase.Columns> builder = new SqlFilterBuilder<DivisionCodeBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(DivisionCodeBase.Columns.DivisionId, id);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }

        private DivisionCode UpdateDivisionCode(dynamic item, string id)
        {
            DivisionCode divisionCode = Find(item.DivisionId ?? id);
            if (divisionCode == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.DivisionId ?? id));

            divisionCode.Description = item.Description ?? divisionCode.Description;

            this.ValidateEntity(divisionCode);

            return divisionCode;
        }

        private DivisionCode CreateDivisionCode(dynamic item)
        {
            DivisionCode divisionCode = Find(item.DivisionId);
            if (divisionCode != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.DivisionId));
            else
                divisionCode = new DivisionCode(this.CompId);

            divisionCode.DivisionId = item.DivisionId;
            divisionCode.Description = item.Description;

            this.ValidateEntity(divisionCode);

            return divisionCode;
        }

        private void MarkToDelete(string id)
        {
            DivisionCode divisionCode = Find(id);
            if (divisionCode == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(DivisionCode entity)
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

        private DivisionCode Find(string id)
        {
            var header = Provider.Items.Find(DivisionCodeBase.Columns.DivisionId, id);
            if (header == null)
            {
                header = EntityProvider.GetEntity<DivisionCode, DivisionCodeProvider>(new string[] { id }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private DivisionCodeProvider Provider { get; } = new DivisionCodeProvider();

        private const string FunctionID = "6449839C-88F2-4D8B-876C-1FF128AE34D5";
        #endregion Properties
    }
}
