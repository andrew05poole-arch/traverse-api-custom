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
    public class ApiARSalesRepController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "salesrepid/{id?}", typeof(SalesRep))]
        public IHttpActionResult Get(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                return Ok(Load(id));
            return Ok(Load(null));
        }

        [ApiRoute(FunctionID, 2f, "salesrepid/{id?}", typeof(SalesRep))]
        public IHttpActionResult Put([FromBody] dynamic body, string id = null)
        {
            List<SalesRep> codes = new List<SalesRep>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var salesCode = UpdateSalesRepCode(item, id);
                if (!codes.Contains(salesCode))
                    codes.Add(salesCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "salesrepid", typeof(SalesRep))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<SalesRep> codes = new List<SalesRep>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1)
                throw new InvalidValueException("Call is ambiguous. ID is provided along wih more than one record.");

            foreach (dynamic item in list)
            {
                var salesCode = CreateSalesRepCode(item);
                this.Provider.Items.Add(salesCode);

                if (!codes.Contains(salesCode))
                    codes.Add(salesCode);
            }
            Provider.Update(CompId);

            return Ok(codes);
        }

        [ApiRoute(FunctionID, 2f, "salesrepid/{id?}", typeof(SalesRep))]
        public IHttpActionResult Delete(string id = null)
        {
            MarkToDelete(id);
            Provider?.Update(CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        private EntityList<SalesRep> Load(string id)
        {
            SqlFilterBuilder<SalesRepBase.Columns> builder = new SqlFilterBuilder<SalesRepBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(SalesRepBase.Columns.SalesRepId, id);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)

                throw new InvalidValueException(string.Format("The id '{0}' could not be found", id));

            return Provider.Items;
        }
        private SalesRep UpdateSalesRepCode(dynamic item, string id)
        {
            SalesRep salesCode = Find(item.SalesRepId ?? id);
            if (salesCode == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.SalesRepId ?? id));

            salesCode.Name = item.Name ?? salesCode.Name;
            salesCode.Addr1 = item.Addr1 ?? salesCode.Addr1;
            salesCode.Addr2 = item.Addr2 ?? salesCode.Addr2;
            salesCode.City = item.City ?? salesCode.City;
            salesCode.Region = item.Region ?? salesCode.Region;
            salesCode.Country = item.Country ?? salesCode.Country;
            salesCode.PostalCode = item.PostalCode ?? salesCode.PostalCode;
            salesCode.IntlPrefix = item.IntlPrefix ?? salesCode.IntlPrefix;
            salesCode.Phone = item.Phone ?? salesCode.Phone;
            salesCode.Fax = item.Fax ?? salesCode.Fax;
            salesCode.EmplId = item.EmplId ?? salesCode.EmplId;
            salesCode.RunCode = item.RunCode ?? salesCode.RunCode;
            salesCode.CommRate = (decimal?)item.CommRate ?? salesCode.CommRate;
            salesCode.PctOf = (byte?)item.PctOf ?? salesCode.PctOf;
            salesCode.BasedOn = (byte?)item.BasedOn ?? salesCode.BasedOn;
            salesCode.PayOnLineItems = item.PayOnLineItems ?? salesCode.PayOnLineItems;
            salesCode.PayOnSalesTax = item.PayOnSalesTax ?? salesCode.PayOnSalesTax;
            salesCode.PayOnFreight = item.PayOnFreight ?? salesCode.PayOnFreight;
            salesCode.PayOnMisc = item.PayOnMisc ?? salesCode.PayOnMisc;
            salesCode.PTDSales = (decimal?)item.PTDSales ?? salesCode.PTDSales;
            salesCode.YTDSales = (decimal?)item.YTDSales ?? salesCode.YTDSales;
            salesCode.LastSalesDate = item.LastSalesDate ?? salesCode.LastSalesDate;
            salesCode.Email = item.Email ?? salesCode.EmplId;
            salesCode.Internet = item.Internet ?? salesCode.Internet;
            salesCode.PayVia = (byte?)item.PayVia ?? salesCode.PayVia;
            salesCode.EarnCode = item.EarnCode ?? salesCode.EarnCode;
            salesCode.VendorId = item.VendorId ?? salesCode.VendorId;

            this.ValidateEntity(salesCode);

            return salesCode;
        }

        private SalesRep CreateSalesRepCode(dynamic item)
        {
            SalesRep salesCode = Find(item.SalesRepId);
            if (salesCode != null)
                throw new InvalidValueException(string.Format("The id '{0}' already exists.", item.SalesRepId));
            else
                salesCode = new SalesRep(this.CompId);
            salesCode.SalesRepId = item.SalesRepId;
            salesCode.Name = item.Name;
            salesCode.Addr1 = item.Addr1;
            salesCode.Addr2 = item.Addr2;
            salesCode.City = item.City;
            salesCode.Region = item.Region;
            salesCode.Country = item.Country;
            salesCode.PostalCode = item.PostalCode;
            salesCode.IntlPrefix = item.IntlPrefix;
            salesCode.Phone = item.Phone;
            salesCode.Fax = item.Fax;
            salesCode.EmplId = item.EmplId;
            salesCode.RunCode = item.RunCode;
            salesCode.CommRate = (decimal?)item.CommRate;
            salesCode.PctOf = (byte?)item.PctOf;
            salesCode.BasedOn = (byte?)item.BasedOn;
            salesCode.PayOnLineItems = item.PayOnLineItems;
            salesCode.PayOnSalesTax = item.PayOnSalesTax;
            salesCode.PayOnFreight = item.PayOnFreight;
            salesCode.PayOnMisc = item.PayOnMisc;
            salesCode.PTDSales = (decimal?)item.PTDSales;
            salesCode.YTDSales = (decimal?)item.YTDSales;
            salesCode.LastSalesDate = item.LastSalesDate;
            salesCode.Email = item.Email;
            salesCode.Internet = item.Internet;
            salesCode.PayVia = (byte)item.PayVia;
            salesCode.EarnCode = item.EarnCode;
            salesCode.VendorId = item.VendorId;

            this.ValidateEntity(salesCode);

            return salesCode;
        }

        private void MarkToDelete(string id)
        {
            SalesRep salesCode = Find(id);
            if (salesCode == null)
                throw new NothingToProcessException(string.Format("The id '{0}' could not be found", id));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(SalesRep entity)
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

        private SalesRep Find(string id)
        {
            var header = Provider.Items.Find(SalesRepBase.Columns.SalesRepId, id);
            if (header == null)
            {
                header = EntityProvider.GetEntity<SalesRep, SalesRepProvider>(new string[] { id }, CompId, null);
                if (header != null)
                    Provider.Items.Add(header);
            }
            return header;
        }
        #endregion Helper Methods

        #region Properties
        private SalesRepProvider Provider { get; } = new SalesRepProvider();

        private const string FunctionID = "5FA0617B-C7E1-4D69-B801-B88E2C351283";
        #endregion Properties
    }
}
