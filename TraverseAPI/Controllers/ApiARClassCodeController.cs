#region Using Directives
using OSI.TraverseApi.Business;
using System.Collections.Generic;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
#endregion Using Directives 

namespace TraverseApi.Controllers
{
    public class ApiARClassCodeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "classcode/{id?}", typeof(CustomerClass))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));

            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "classcode/{id?}", typeof(CustomerClass))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<CustomerClass> codes = new List<CustomerClass>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var classCode = UpdateCustomerClass(item, id);
                if (!codes.Contains(classCode))
                    codes.Add(classCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "classcode", typeof(CustomerClass))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<CustomerClass> codes = new List<CustomerClass>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var classCode = CreateCustomerClass(item);
                this.Provider.Items.Add(classCode);

                if (!codes.Contains(classCode))
                    codes.Add(classCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "classcode/{id?}", typeof(CustomerClass))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<CustomerClass> Load(string id)
        {
            SqlFilterBuilder<CustomerClassBase.Columns> builder = new SqlFilterBuilder<CustomerClassBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(CustomerClassBase.Columns.ClassId, id);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }

        private CustomerClass UpdateCustomerClass(dynamic item, string id)
        {
            CustomerClass classCode = Find(item.ClassId ?? id);
            if (classCode == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.ClassId ?? id));

            classCode.Description = item.Description ?? classCode.Description;

            this.ValidateEntity(classCode);

                return classCode;
        }

        private CustomerClass CreateCustomerClass(dynamic item)
        {
            CustomerClass classCode = Find(item.ClassId);
            if (classCode != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.ClassId));
            else
                classCode = new CustomerClass(this.CompId);

            classCode.ClassId = item.ClassId;
            classCode.Description = item.Description;

            this.ValidateEntity(classCode);

            return classCode;
        }

        private void MarkToDelete(string id)
        {
            CustomerClass classCode = Find(id);
            if (classCode == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(CustomerClass entity)
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

        private CustomerClass Find(string id)
        {
            var header = Provider.Items.Find(CustomerClassBase.Columns.ClassId, id);
            if (header == null)
            {
                header = EntityProvider.GetEntity<CustomerClass, CustomerClassProvider>(new string[] { id }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private CustomerClassProvider Provider { get; } = new CustomerClassProvider();

        private const string FunctionID = "5199CF70-A861-42CD-B03A-9DC53742A12E"; 
        #endregion Properties
    }
}