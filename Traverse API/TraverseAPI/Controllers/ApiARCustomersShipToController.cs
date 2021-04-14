#region Using Directives
using System.Collections.Generic;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
#endregion Using Directives 

namespace TraverseApi.Controllers
{
    public class ApiARCustomersShipToController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "customershipto/{ids?}", typeof(CustomerShipTo))]
        public IHttpActionResult Get(string ids = null)
        {
            string custId = string.Empty;
            string shipId = string.Empty;

            if (!string.IsNullOrEmpty(ids)) {
                string[] idSplit = ids.Split('-');
                custId = idSplit[0];
                shipId = idSplit[1];
            }

            if (!string.IsNullOrEmpty(shipId) 
                && !string.IsNullOrEmpty(custId))
                return Ok(Load(shipId, custId));

            return Ok(Load(null,null));
        }

        [ApiRoute(FunctionID, 2f, "customershipto/{ids?}", typeof(CustomerShipTo))]
        public IHttpActionResult Put([FromBody] dynamic body, string ids = null)
        {
            string custId = string.Empty;
            string shipId = string.Empty;

            if (!string.IsNullOrEmpty(ids))
            {
                string[] idSplit = ids.Split('-');
                custId = idSplit[0];
                shipId = idSplit[1];
            }

            List<CustomerShipTo> customersshipto = new List<CustomerShipTo>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(shipId))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {              

                var customershipto = UpdateCustomerShipTo(item, shipId, custId);
                if (!customersshipto.Contains(customershipto))
                    customersshipto.Add(customershipto);
            }
            Provider.Update(CompId);

            return Ok(customersshipto);
        }

        [ApiRoute(FunctionID, 2f, "customershipto", typeof(CustomerShipTo))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<CustomerShipTo> customersshipto = new List<CustomerShipTo>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var customershipto = CreateCustomerShipTo(item);
                this.Provider.Items.Add(customershipto);

                if (!customersshipto.Contains(customershipto))
                    customersshipto.Add(customershipto);
            }
            Provider.Update(CompId);

            return Ok(customersshipto);
        }

        [ApiRoute(FunctionID, 2f, "customershipto/{ids?}", typeof(CustomerShipTo))]
        public IHttpActionResult Delete(string ids = null)
        {
            string custId = string.Empty;
            string shipId = string.Empty;

            if (!string.IsNullOrEmpty(ids))
            {
                string[] idSplit = ids.Split('-');
                custId = idSplit[0];
                shipId = idSplit[1];
            }

            MarkToDelete(shipId,custId);
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<CustomerShipTo> Load(string shipId, string custId)
        {
            SqlFilterBuilder<CustomerShipToBase.Columns> builder = new SqlFilterBuilder<CustomerShipToBase.Columns>();
            if (!string.IsNullOrEmpty(shipId)
                && !string.IsNullOrEmpty(custId))
            {
                builder.AppendEquals(CustomerShipToBase.Columns.CustId, custId);
                builder.AppendEquals(CustomerShipToBase.Columns.ShiptoId, shipId);
            }
            Provider.CompId = CompId;
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}-{1}' could not be found", custId,shipId));

            return Provider.Items;
        }

        private CustomerShipTo UpdateCustomerShipTo(dynamic item, string shipId, string custId)
        {
            CustomerShipTo customershipto = Find(item.ShiptoId ?? shipId, item.CustId ?? custId);
            if (customershipto == null)
                throw new InvalidValueException(string.Format("The id '{0}-{1}' could not be found", item.CustId ?? custId, item.ShiptoId ?? shipId));

            customershipto.ShiptoId = item.ShiptoId ?? customershipto.ShiptoId;
            customershipto.ShiptoName = item.ShiptoName ?? customershipto.ShiptoName;
            customershipto.Addr1 = item.Addr1 ?? customershipto.Addr1;
            customershipto.Addr2 = item.Addr2 ?? customershipto.Addr2;
            customershipto.City = item.City ?? customershipto.City;
            customershipto.Region = item.Region ?? customershipto.Region;
            customershipto.Country = item.Country ?? customershipto.Country;
            customershipto.PostalCode = item.PostalCode ?? customershipto.PostalCode;
            customershipto.IntlPrefix = item.IntlPrefix ?? customershipto.IntlPrefix;
            customershipto.Phone = item.Phone ?? customershipto.Phone;
            customershipto.Fax = item.Fax ?? customershipto.Fax;
            customershipto.Attn = item.Attn ?? customershipto.Attn;
            customershipto.ShipVia = item.ShipVia ?? customershipto.ShipVia;
            customershipto.TaxLocId = item.TaxLocId ?? customershipto.TaxLocId;
            customershipto.TerrId = item.TerrId ?? customershipto.TerrId;
            customershipto.DistCode = item.DistCode ?? customershipto.DistCode;
            customershipto.Email = item.Email ?? customershipto.Email;
            customershipto.Internet = item.Internet ?? customershipto.Internet;
            customershipto.AddressType = item.AddressType ?? customershipto.AddressType;
            customershipto.Phone1 = item.Phone1 ?? customershipto.Phone1;
            customershipto.Phone2 = item.Phone2 ?? customershipto.Phone2;
            customershipto.Rep1Id = item.Rep1Id ?? customershipto.Rep1Id;
            customershipto.Rep2Id = item.Rep2Id ?? customershipto.Rep2Id;
            customershipto.Rep1PctInvc = (decimal?)item.Rep1PctInvc ?? customershipto.Rep1PctInvc;
            customershipto.Rep2PctInvc = (decimal?)item.Rep2PctInvc ?? customershipto.Rep2PctInvc;
            customershipto.ShipMethod = item.ShipMethod ?? customershipto.ShipMethod;
            customershipto.TaxGrpId2 = item.TaxGrpId2 ?? customershipto.TaxGrpId2;

            this.ValidateEntity(customershipto);

            return customershipto;
        }

        private CustomerShipTo CreateCustomerShipTo(dynamic item)
        {
            CustomerShipTo customershipto = Find(item.ShiptoId, item.CustId);
            if (customershipto != null)
                throw new InvalidValueException(string.Format("The id '{0}-{1}' already exists.", item.CustId, item.ShiptoId));
            else
                customershipto = new CustomerShipTo(this.CompId);

            customershipto.CustId = item.CustId;
            customershipto.ShiptoId = item.ShiptoId;
            customershipto.ShiptoName = item.ShiptoName;
            customershipto.Addr1 = item.Addr1;
            customershipto.Addr2 = item.Addr2;
            customershipto.City = item.City;
            customershipto.Region = item.Region;
            customershipto.Country = item.Country;
            customershipto.PostalCode = item.PostalCode;
            customershipto.IntlPrefix = item.IntlPrefix;
            customershipto.Phone = item.Phone;
            customershipto.Fax = item.Fax;
            customershipto.Attn = item.Attn;
            customershipto.ShipVia = item.ShipVia;
            customershipto.TaxLocId = item.TaxLocId;
            customershipto.TerrId = item.TerrId;
            customershipto.DistCode = item.DistCode;
            customershipto.Email = item.Email;
            customershipto.Internet = item.Internet;
            customershipto.AddressType = item.AddressType;
            customershipto.Phone1 = item.Phone1;
            customershipto.Phone2 = item.Phone2;
            customershipto.Rep1Id = item.Rep1Id;
            customershipto.Rep2Id = item.Rep2Id;
            customershipto.Rep1PctInvc = (decimal)item.Rep1PctInvc;
            customershipto.Rep2PctInvc = (decimal)item.Rep2PctInvc;
            customershipto.ShipMethod = item.ShipMethod;
            customershipto.TaxGrpId2 = item.TaxGrpId2;

            this.ValidateEntity(customershipto);

            return customershipto;
        }

        private void MarkToDelete(string shipId, string custId)
        {
            CustomerShipTo customer = Find(shipId, custId);
            if (customer == null)
                throw new NothingToProcessException(string.Format("The id '{0}-{1}' could not be found", custId, shipId));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(CustomerShipTo entity)
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

        private CustomerShipTo Find(string shipId, string custId)
        {
            var header = Provider.Items.Find(CustomerShipToBase.Columns.ShiptoId, shipId);
            if (header == null)
            {
                header = EntityProvider.GetEntity<CustomerShipTo, CustomerShipToProvider>(new string[] {custId, shipId }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private CustomerShipToProvider Provider { get; } = new CustomerShipToProvider();

        private const string FunctionID = "4FE336C1-83D6-4128-8A07-14327F26C240";
        #endregion Properties
    }
}