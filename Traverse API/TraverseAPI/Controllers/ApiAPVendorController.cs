#region Using Directives
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
#endregion Using Directives

namespace TraverseApi.Controllers
{
    public class ApiAPVendorController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "vendor/{id?}", typeof(Vendor))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));

            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "vendor/{id?}", typeof(Vendor))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<Vendor> vendors = new List<Vendor>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var vendor = UpdateVendor(item, id);
                if (!vendors.Contains(vendor))
                    vendors.Add(vendor);
            }
            Provider.Update(CompId);

            return Ok(vendors);
        }

        [ApiRoute(FunctionID, 2f, "vendor", typeof(Vendor))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<Vendor> Vendors = new List<Vendor>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var vendor = CreateVendor(item);
                this.Provider.Items.Add(vendor);

                if (!Vendors.Contains(vendor))
                    Vendors.Add(vendor);
            }
            this.Provider.Update(CompId);

            return Ok(Vendors);
        }

        [ApiRoute(FunctionID, 2f, "vendor/{id?}", typeof(Vendor))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<Vendor> Load(string id)
        {
            SqlFilterBuilder<VendorBase.Columns> builder = new SqlFilterBuilder<VendorBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(VendorBase.Columns.VendorId, id);
            Provider.CompId = CompId;
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }

        private Vendor UpdateVendor(dynamic item, string id)
        {
            Vendor vendor = Find(item.VendorId ?? id);
            if (vendor == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.VendorId ?? id));

            vendor.Name = item.Name ?? vendor.Name;
            vendor.Contact = item.Contact ?? vendor.Contact;
            vendor.Addr1 = item.Addr1 ?? vendor.Addr1;
            vendor.Addr2 = item.Addr2 ?? vendor.Addr2;
            vendor.City = item.City ?? vendor.City;
            vendor.Region = item.Region ?? vendor.Region;
            vendor.Country = item.Country ?? vendor.Country;
            vendor.PostalCode = item.PostalCode ?? vendor.PostalCode;
            vendor.IntlPrefix = item.IntlPrefix ?? vendor.IntlPrefix;
            vendor.Phone = item.Phone ?? vendor.Phone;
            vendor.FAX = item.FAX ?? vendor.FAX;
            vendor.OurAcctNum = item.OurAcctNum ?? vendor.OurAcctNum;
            vendor.PayToName = item.PayToName ?? vendor.PayToName;
            vendor.PayToAttention = item.PayToAttention ?? vendor.PayToAttention;
            vendor.PayToAddr1 = item.PayToAddr1 ?? vendor.PayToAddr1;
            vendor.PayToAddr2 = item.PayToAddr2 ?? vendor.PayToAddr2;
            vendor.PayToCity = item.PayToCity ?? vendor.PayToCity;
            vendor.PayToRegion = item.PayToRegion ?? vendor.PayToRegion;
            vendor.PayToCountry = item.PayToCountry ?? vendor.PayToCountry;
            vendor.PayToPostalCode = item.PayToPostalCode ?? vendor.PayToPostalCode;
            vendor.PayToIntlPrefix = item.PayToIntlPrefix ?? vendor.PayToIntlPrefix;
            vendor.PayToPhone = item.PayToPhone ?? vendor.PayToPhone;
            vendor.Ten99FormCode = item.Ten99FormCode ?? vendor.Ten99FormCode;
            vendor.Ten99RecipientId = item.Ten99RecipientId ?? vendor.Ten99RecipientId;
            vendor.Ten99FieldIndicator = item.Ten99FieldIndicator ?? vendor.Ten99FieldIndicator;
            vendor.Ten99ForeignAddrYN = item.Ten99ForeignAddrYN ?? vendor.Ten99ForeignAddrYN;
            vendor.SecondTINNotYN = item.SecondTINNotYN ?? vendor.SecondTINNotYN;
            vendor.GLAcct = item.GLAcct ?? vendor.GLAcct;
            vendor.PriorityCode = item.PriorityCode ?? vendor.PriorityCode;
            vendor.VendorHoldYN = item.VendorHoldYN ?? vendor.VendorHoldYN;
            vendor.TempYN = item.TempYN ?? vendor.TempYN;
            vendor.VendorClass = item.VendorClass ?? vendor.VendorClass;
            vendor.TermsCode = item.TermsCode ?? vendor.TermsCode;
            vendor.DivisionCode = item.DivisionCode ?? vendor.DivisionCode;
            vendor.DistCode = item.DistCode ?? vendor.DistCode;
            vendor.TaxableYN = item.TaxableYN ?? vendor.TaxableYN;
            vendor.Memo = item.Memo ?? vendor.Memo;
            vendor.CurrencyId = item.CurrencyId ?? vendor.CurrencyId;
            vendor.TaxGrpId = item.TaxGrpId ?? vendor.TaxGrpId;
            vendor.Email = item.Email ?? vendor.Email;
            vendor.Internet = item.Internet ?? vendor.Internet;
            vendor.LastPurchDate = item.LastPurchDate ?? vendor.LastPurchDate;
            vendor.LastPurchNum = item.LastPurchNum ?? vendor.LastPurchNum;
            vendor.LastPmtDate = item.LastPmtDate ?? vendor.LastPmtDate;
            vendor.LastCheckNum = item.LastCheckNum ?? vendor.LastCheckNum;
            vendor.LastPurchAmt = (decimal?)item.LastPurchAmt ?? vendor.LastPurchAmt;
            vendor.LastPmtAmt = (decimal?)item.LastPmtAmt ?? vendor.LastPmtAmt;
            vendor.GrossDue = (decimal?)item.GrossDue ?? vendor.GrossDue;
            vendor.Prepaid = (decimal?)item.Prepaid ?? vendor.Prepaid;
            vendor.DfltTransAllocId = item.DfltTransAllocId ?? vendor.DfltTransAllocId;
            vendor.Status = (byte?)item.Status ?? vendor.Status;
            vendor.DeliveryType = (byte?)item.DeliveryType ?? vendor.DeliveryType;
            vendor.BankAcctNum = item.BankAcctNum ?? vendor.BankAcctNum;
            vendor.MaskValue = item.MaskValue ?? vendor.MaskValue;
            vendor.RoutingCode = item.RoutingCode ?? vendor.RoutingCode;
            vendor.DefaultPayBankId = item.DefaultPayBankId ?? vendor.DefaultPayBankId;
            vendor.ChkOpt = (byte?)item.ChkOpt ?? vendor.ChkOpt;
            vendor.BankAccountType = (byte?)item.BankAccountType ?? vendor.BankAccountType;
            vendor.TaxPayerType1 = item.TaxPayerType1 ?? vendor.TaxPayerType1;
            vendor.TaxPayerType2 = item.TaxPayerType2 ?? vendor.TaxPayerType2;
            vendor.TaxId1 = item.TaxId1 ?? vendor.TaxId1;
            vendor.TaxId2 = item.TaxId2 ?? vendor.TaxId2;
            vendor.TaxGrpId2 = item.TaxGrpId2 ?? vendor.TaxGrpId2;


            this.ValidateEntity(vendor);

            return vendor;
        }

        private Vendor CreateVendor(dynamic item)
        {
            Vendor vendor = Find(item.VendorId);
            if (vendor != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.VendorId));
            else
                vendor = new Vendor(this.CompId);

            vendor.VendorId = item.VendorId;
            vendor.Name = item.Name;
            vendor.Contact = item.Contact;
            vendor.Addr1 = item.Addr1;
            vendor.Addr2 = item.Addr2;
            vendor.City = item.City;
            vendor.Region = item.Region;
            vendor.Country = item.Country;
            vendor.PostalCode = item.PostalCode;
            vendor.IntlPrefix = item.IntlPrefix;
            vendor.Phone = item.Phone;
            vendor.FAX = item.FAX;
            vendor.OurAcctNum = item.OurAcctNum;
            vendor.PayToName = item.PayToName;
            vendor.PayToAttention = item.PayToAttention;
            vendor.PayToAddr1 = item.PayToAddr1;
            vendor.PayToAddr2 = item.PayToAddr2;
            vendor.PayToCity = item.PayToCity;
            vendor.PayToRegion = item.PayToRegion;
            vendor.PayToCountry = item.PayToCountry;
            vendor.PayToPostalCode = item.PayToPostalCode;
            vendor.PayToIntlPrefix = item.PayToIntlPrefix;
            vendor.PayToPhone = item.PayToPhone;
            vendor.Ten99FormCode = item.Ten99FormCode;
            vendor.Ten99RecipientId = item.Ten99RecipientId;
            vendor.Ten99FieldIndicator = item.Ten99FieldIndicator;
            vendor.Ten99ForeignAddrYN = item.Ten99ForeignAddrYN;
            vendor.SecondTINNotYN = item.SecondTINNotYN;
            vendor.GLAcct = item.GLAcct;
            vendor.PriorityCode = item.PriorityCode;
            vendor.VendorHoldYN = item.VendorHoldYN;
            vendor.TempYN = item.TempYN;
            vendor.VendorClass = item.VendorClass;
            vendor.TermsCode = item.TermsCode;
            vendor.DivisionCode = item.DivisionCode;
            vendor.DistCode = item.DistCode;
            vendor.TaxableYN = item.TaxableYN;
            vendor.Memo = item.Memo;
            vendor.CurrencyId = item.CurrencyId;
            vendor.TaxGrpId = item.TaxGrpId;
            vendor.Email = item.Email;
            vendor.Internet = item.Internet;
            vendor.LastPurchDate = item.LastPurchDate;
            vendor.LastPurchNum = item.LastPurchNum;
            vendor.LastPmtDate = item.LastPmtDate;
            vendor.LastCheckNum = item.LastCheckNum;
            vendor.LastPurchAmt = (decimal)item.LastPurchAmt;
            vendor.LastPmtAmt = (decimal?)item.LastPmtAmt ?? 0;
            vendor.GrossDue = (decimal)item.GrossDue;
            vendor.Prepaid = (decimal)item.Prepaid;
            vendor.DfltTransAllocId = item.DfltTransAllocId;
            vendor.Status = (byte)item.Status;
            vendor.DeliveryType = (byte)item.DeliveryType;
            vendor.BankAcctNum = item.BankAcctNum;
            vendor.MaskValue = item.MaskValue;
            vendor.RoutingCode = item.RoutingCode;
            vendor.DefaultPayBankId = item.DefaultPayBankId;
            vendor.ChkOpt = (byte)item.ChkOpt;
            vendor.BankAccountType = (byte)item.BankAccountType;
            vendor.TaxPayerType1 = item.TaxPayerType1;
            vendor.TaxPayerType2 = item.TaxPayerType2;
            vendor.TaxId1 = item.TaxId1;
            vendor.TaxId2 = item.TaxId2;
            vendor.TaxGrpId2 = item.TaxGrpId2;

            this.ValidateEntity(vendor);

            return vendor;
        }

        private void MarkToDelete(string id)
        {
            Vendor vendor = Find(id);
            if (vendor == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(Vendor entity)
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

        private Vendor Find(string id)
        {
            var header = Provider.Items.Find(VendorBase.Columns.VendorId, id);
            if (header == null)
            {
                header = EntityProvider.GetEntity<Vendor, VendorProvider>(new string[] { id }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private VendorProvider Provider { get; } = new VendorProvider();

        private const string FunctionID = "1AB8E9D6-9968-4CFD-84EB-407ECB081810";
        #endregion Properties
    }
}