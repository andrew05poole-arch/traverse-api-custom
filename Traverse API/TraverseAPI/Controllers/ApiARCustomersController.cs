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
    public class ApiARCustomersController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "customer/{id?}", typeof(Customer))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));

            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "customer/{id?}", typeof(Customer))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<Customer> customers = new List<Customer>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var customer = UpdateCustomer(item, id);
                if (!customers.Contains(customer))
                    customers.Add(customer);
            }
            Provider.Update(CompId);

            return Ok(customers);
        }

        [ApiRoute(FunctionID, 2f, "customer", typeof(Customer))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<Customer> customers = new List<Customer>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var customer = CreateCustomer(item);
                this.Provider.Items.Add(customer);

                if (!customers.Contains(customer))
                    customers.Add(customer);
            }
            Provider.Update(CompId);

            return Ok(customers);
        }

        [ApiRoute(FunctionID, 2f, "customer/{id?}", typeof(Customer))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<Customer> Load(string id)
        {
            SqlFilterBuilder<CustomerBase.Columns> builder = new SqlFilterBuilder<CustomerBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(CustomerBase.Columns.CustId, id);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }

        private Customer UpdateCustomer(dynamic item, string id)
        {
            Customer customer = Find(item.CustId ?? id);
            if (customer == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.CustId ?? id));

            customer.CustName = item.CustName ?? customer.CustName;
            customer.Contact = item.Contact ?? customer.Contact;
            customer.Addr1 = item.Addr1 ?? customer.Addr1;
            customer.Addr2 = item.Addr2 ?? customer.Addr2;
            customer.City = item.City ?? customer.City;
            customer.Region = item.Region ?? customer.Region;
            customer.Country = item.Country ?? customer.Country;
            customer.PostalCode = item.PostalCode ?? customer.PostalCode;
            customer.ShipZone = item.ShipZone ?? customer.ShipZone;
            customer.IntlPrefix = item.IntlPrefix ?? customer.IntlPrefix;
            customer.Phone = item.Phone ?? customer.Phone;
            customer.Fax = item.Fax ?? customer.Fax;
            customer.Attn = item.Attn ?? customer.Attn;
            customer.ClassId = item.ClassId ?? customer.ClassId;
            customer.SalesRepId1 = item.SalesRepId1 ?? customer.SalesRepId1;
            customer.SalesRepId2 = item.SalesRepId2 ?? customer.SalesRepId2;
            customer.Rep1PctInvc = (decimal?)item.Rep1PctInvc ?? customer.Rep1PctInvc;
            customer.Rep2PctInvc = (decimal?)item.Rep2PctInvc ?? customer.Rep2PctInvc;
            customer.TermsCode = item.TermsCode ?? customer.TermsCode;
            customer.PmtMethod = item.PmtMethod ?? customer.PmtMethod;
            customer.GroupCode = item.GroupCode ?? customer.GroupCode;
            customer.StmtInvcCode = (byte?)item.StmtInvcCode ?? customer.StmtInvcCode;
            customer.AcctType = (byte?)item.AcctType ?? customer.AcctType;
            customer.PriceCode = item.PriceCode ?? customer.PriceCode;
            customer.DistCode = item.DistCode ?? customer.DistCode;
            customer.CalcFinch = item.CalcFinch ?? customer.CalcFinch;
            customer.CreditLimit = (decimal?)item.CreditLimit ?? customer.CreditLimit;
            customer.CreditHold = item.CreditHold ?? customer.CreditHold;
            customer.PartialShip = item.PartialShip ?? customer.PartialShip;
            customer.AutoCreditHold = item.AutoCreditHold ?? customer.AutoCreditHold;
            customer.TaxLocId = item.TaxLocId ?? customer.TaxLocId;
            customer.Taxable = item.Taxable ?? customer.Taxable;
            customer.TaxExemptId = item.TaxExemptId ?? customer.TaxExemptId;
            customer.CurrencyId = item.CurrencyId ?? customer.CurrencyId;
            customer.TerrId = item.TerrId ?? customer.TerrId;
            customer.CcCompYn = item.CcCompYn ?? customer.CcCompYn;
            customer.CustLevel = item.CustLevel ?? customer.CustLevel;
            customer.Email = item.Email ?? customer.Email;
            customer.Internet = item.Internet ?? customer.Internet;
            customer.NewFinch = (decimal?)item.NewFinch ?? customer.NewFinch;
            customer.UnpaidFinch = (decimal?)item.UnpaidFinch ?? customer.UnpaidFinch;
            customer.CurAmtDue = (decimal?)item.CurAmtDue ?? customer.CurAmtDue;
            customer.CurAmtDueFgn = (decimal?)item.CurAmtDueFgn ?? customer.CurAmtDueFgn;
            customer.BalAge1 = (decimal?)item.BalAge1 ?? customer.BalAge1;
            customer.BalAge2 = (decimal?)item.BalAge2 ?? customer.BalAge2;
            customer.BalAge3 = (decimal?)item.BalAge3 ?? customer.BalAge3;
            customer.BalAge4 = (decimal?)item.BalAge4 ?? customer.BalAge4;
            customer.UnapplCredit = (decimal?)item.UnapplCredit ?? customer.UnapplCredit;
            customer.FirstSaleDate = item.FirstSaleDate ?? customer.FirstSaleDate;
            customer.LastSaleDate = item.LastSaleDate ?? customer.LastSaleDate;
            customer.LastSaleAmt = (decimal?)item.LastSaleAmt ?? customer.LastSaleAmt;
            customer.LastSaleInvc = item.LastSaleInvc ?? customer.LastSaleInvc;
            customer.LastPayDate = item.LastPayDate ?? customer.LastPayDate;
            customer.LastPayAmt = (decimal?)item.LastPayAmt ?? customer.LastPayAmt;
            customer.LastPayCheckNum = item.LastPayCheckNum ?? customer.LastPayCheckNum;
            customer.HighBal = (decimal?)item.HighBal ?? customer.HighBal;
            customer.CreditStatus = item.CreditStatus ?? customer.CreditStatus;
            customer.WebDisplInQtyYn = item.WebDisplInQtyYn ?? customer.WebDisplInQtyYn;
            customer.Phone1 = item.Phone1 ?? customer.Phone1;
            customer.Phone2 = item.Phone2 ?? customer.Phone2;
            customer.AllowCharge = item.AllowCharge ?? customer.AllowCharge;
            customer.Status = (byte?)item.Status ?? customer.Status;
            customer.BillToId = item.BillToId ?? customer.BillToId;
            customer.PONumberRequiredYn = item.PONumberRequiredYn ?? customer.PONumberRequiredYn;
            customer.TaxCertExpDate = item.TaxCertExpDate ?? customer.TaxCertExpDate;
            customer.TaxPayerType1 = item.TaxPayerType1 ?? customer.TaxPayerType1;
            customer.TaxPayerType2 = item.TaxPayerType2 ?? customer.TaxPayerType2;
            customer.TaxId1 = item.TaxId1 ?? customer.TaxId1;
            customer.TaxId2 = item.TaxId2 ?? customer.TaxId2;
            customer.TaxGrpId2 = item.TaxGrpId2 ?? customer.TaxGrpId2;

            this.ValidateEntity(customer);

            return customer;
        }

        private Customer CreateCustomer(dynamic item)
        {
            Customer customer = Find(item.CustId);
            if (customer != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.CustId));
            else
                customer = new Customer(this.CompId);

            customer.CustId = item.CustId;
            customer.CustName = item.CustName;
            customer.Contact = item.Contact;
            customer.Addr1 = item.Addr1;
            customer.Addr2 = item.Addr2;
            customer.City = item.City;
            customer.Region = item.Region;
            customer.Country = item.Country;
            customer.PostalCode = item.PostalCode;
            customer.ShipZone = item.ShipZone;
            customer.IntlPrefix = item.IntlPrefix;
            customer.Phone = item.Phone;
            customer.Fax = item.Fax;
            customer.Attn = item.Attn;
            customer.ClassId = item.ClassId;
            customer.SalesRepId1 = item.SalesRepId1;
            customer.SalesRepId2 = item.SalesRepId2;
            customer.Rep1PctInvc = (decimal)item.Rep1PctInvc;
            customer.Rep2PctInvc = (decimal)item.Rep2PctInvc;
            customer.TermsCode = item.TermsCode;
            customer.PmtMethod = item.PmtMethod;
            customer.GroupCode = item.GroupCode;
            customer.StmtInvcCode = (byte)item.StmtInvcCode;
            customer.AcctType = (byte)item.AcctType;
            customer.PriceCode = item.PriceCode;
            customer.DistCode = item.DistCode;
            customer.CalcFinch = item.CalcFinch;
            customer.CreditLimit = (decimal)item.CreditLimit;
            customer.CreditHold = item.CreditHold;
            customer.PartialShip = item.PartialShip;
            customer.AutoCreditHold = item.AutoCreditHold;
            customer.TaxLocId = item.TaxLocId;
            customer.Taxable = item.Taxable;
            customer.TaxExemptId = item.TaxExemptId;
            customer.CurrencyId = item.CurrencyId;
            customer.TerrId = item.TerrId;
            customer.CcCompYn = item.CcCompYn;
            customer.CustLevel = item.CustLevel;
            customer.Email = item.Email;
            customer.Internet = item.Internet;
            customer.NewFinch = (decimal)item.NewFinch;
            customer.UnpaidFinch = (decimal)item.UnpaidFinch;
            customer.CurAmtDue = (decimal)item.CurAmtDue;
            customer.CurAmtDueFgn = (decimal)item.CurAmtDueFgn;
            customer.BalAge1 = (decimal)item.BalAge1;
            customer.BalAge2 = (decimal)item.BalAge2;
            customer.BalAge3 = (decimal)item.BalAge3;
            customer.BalAge4 = (decimal)item.BalAge4;
            customer.UnapplCredit = (decimal)item.UnapplCredit;
            customer.FirstSaleDate = item.FirstSaleDate;
            customer.LastSaleDate = item.LastSaleDate;
            customer.LastSaleAmt = (decimal)item.LastSaleAmt;
            customer.LastSaleInvc = item.LastSaleInvc;
            customer.LastPayDate = item.LastPayDate;
            customer.LastPayAmt = (decimal)item.LastPayAmt;
            customer.LastPayCheckNum = item.LastPayCheckNum;
            customer.HighBal = (decimal)item.HighBal;
            customer.CreditStatus = item.CreditStatus;
            customer.WebDisplInQtyYn = item.WebDisplInQtyYn;
            customer.Phone1 = item.Phone1;
            customer.Phone2 = item.Phone2;
            customer.AllowCharge = item.AllowCharge;
            customer.Status = (byte)item.Status;
            customer.BillToId = item.BillToId;
            customer.PONumberRequiredYn = item.PONumberRequiredYn;
            customer.TaxCertExpDate = item.TaxCertExpDate;
            customer.TaxPayerType1 = item.TaxPayerType1;
            customer.TaxPayerType2 = item.TaxPayerType2;
            customer.TaxId1 = item.TaxId1;
            customer.TaxId2 = item.TaxId2;
            customer.TaxGrpId2 = item.TaxGrpId2;


            this.ValidateEntity(customer);

            return customer;
        }

        private void MarkToDelete(string id)
        {
            Customer customer = Find(id);
            if (customer == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(Customer entity)
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

        private Customer Find(string id)
        {
            var header = Provider.Items.Find(CustomerBase.Columns.CustId, id);
            if (header == null)
            {
                header = EntityProvider.GetEntity<Customer, CustomerProvider>(new string[] { id }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private CustomerProvider Provider { get; } = new CustomerProvider();

        private const string FunctionID = "19432034-BF3E-411A-B410-8362ABBE4AA6";
        #endregion Properties
    }
}