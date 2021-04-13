#region Using Directives
using OSI.TraverseApi.Business;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.PurchaseOrder.Controllers
{
    public class ApiPoShipToAddressController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "shipto/{shiptoid?}", typeof(ShipTo))]
        public IHttpActionResult Get(string shipToId = null)
        {
            return Ok(Load(shipToId));
        }

        [ApiRoute(FunctionID, 2f, "shipto/{shiptoid?}", typeof(ShipTo))]
        public IHttpActionResult Put([FromBody] dynamic body, string shipToId = null)
        {
            List<ShipTo> shipToList = new List<ShipTo>();
            object[] list;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && shipToId != null)
                throw new InvalidValueException("Call is ambiguous. Ship To ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var requisition = UpdateShipTo(item, shipToId);
                if (!shipToList.Contains(requisition))
                    shipToList.Add(requisition);
            }
            this.Provider?.Update(this.CompId);

            return Ok(shipToList);
        }

        [ApiRoute(FunctionID, 2f, "shipto/{shiptoid?}", typeof(ShipTo))]
        public IHttpActionResult Add([FromBody] dynamic body, string shipToId = null)
        {
            List<ShipTo> shipToList = new List<ShipTo>();
            object[] list;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && shipToId != null)
                throw new InvalidValueException("Call is ambiguous. Ship To ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var shipTo = CreateShipTo(item, shipToId);
                this.Provider.Items.Add(shipTo);

                if (!shipToList.Contains(shipTo))
                    shipToList.Add(shipTo);
            }
            this.Provider?.Update(this.CompId);

            return Ok(shipToList);
        }

        [ApiRoute(FunctionID, 2f, "shipto/{shiptoid}", typeof(ShipTo))]
        public IHttpActionResult Delete(string shipToId = null)
        {
            this.MarkToDelete(shipToId);
            this.Provider?.Update(this.CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        private EntityList<ShipTo> Load(string shipToId)
        {
            SqlFilterBuilder<ShipToBase.Columns> builder = new SqlFilterBuilder<ShipToBase.Columns>();
            if (!string.IsNullOrEmpty(shipToId))
                builder.AppendEquals(ShipToBase.Columns.ShiptoId, shipToId);
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
            {
                if (shipToId != null)
                    throw new InvalidValueException(string.Format("Ship To ID '{0}' could not be found.", shipToId));
                else
                    throw new InvalidValueException("No results were found.");
            }
            return Provider.Items;
        }

        private ShipTo UpdateShipTo(dynamic bodyItem, string shipToId)
        {
            ShipTo shipTo = this.Find(bodyItem.ShiptoId ?? shipToId);
            if (shipTo == null)
                throw new InvalidValueException(string.Format("Ship To ID '{0}' could not be found.", bodyItem.ShiptoId ?? shipToId));

            shipTo.PropertyChanged += ShipTo_PropertyChanged;
            shipTo.ShiptoId = bodyItem.ShiptoId ?? shipToId;
            shipTo.ShiptoName = bodyItem.ShiptoName ?? shipTo.ShiptoName;
            shipTo.Addr1 = bodyItem.Addr1 ?? shipTo.Addr1;
            shipTo.Addr2 = bodyItem.Addr2 ?? shipTo.Addr2;
            shipTo.City = bodyItem.City ?? shipTo.City;
            shipTo.Region = bodyItem.Region ?? shipTo.Region;
            shipTo.Country = bodyItem.Country ?? shipTo.Country;
            shipTo.PostalCode = bodyItem.PostalCode ?? shipTo.PostalCode;
            shipTo.Phone = bodyItem.Phone ?? shipTo.Phone;
            shipTo.Fax = bodyItem.Fax ?? shipTo.Fax;
            shipTo.Attn = bodyItem.Attn ?? shipTo.Attn;
            shipTo.ShipVia = bodyItem.ShipVia ?? shipTo.ShipVia;
            shipTo.TaxLocId = bodyItem.TaxLocId ?? shipTo.TaxLocId;
            shipTo.DistCode = bodyItem.DistCode ?? shipTo.DistCode;
            shipTo.Email = bodyItem.Email ?? shipTo.Email;
            shipTo.Internet = bodyItem.Internet ?? shipTo.Internet;
            shipTo.PropertyChanged -= ShipTo_PropertyChanged;

            this.ValidateEntity(shipTo);
            return shipTo;
        }

        private ShipTo CreateShipTo(dynamic bodyItem, string shipToId)
        {
            ShipTo shipTo = this.Find(bodyItem.ShiptoId ?? shipToId);
            if (shipTo != null)
                throw new InvalidValueException(string.Format("Ship To ID '{0}' already exists.", bodyItem.ShiptoId ?? shipToId));
            else
                shipTo = new ShipTo(this.CompId);

            shipTo.PropertyChanged += ShipTo_PropertyChanged;
            shipTo.ShiptoId = bodyItem.ShiptoId ?? shipToId;
            shipTo.ShiptoName = bodyItem.ShiptoName;
            shipTo.Addr1 = bodyItem.Addr1;
            shipTo.Addr2 = bodyItem.Addr2;
            shipTo.City = bodyItem.City;
            shipTo.Region = bodyItem.Region;
            shipTo.Country = bodyItem.Country;
            shipTo.PostalCode = bodyItem.PostalCode;
            shipTo.Phone = bodyItem.Phone;
            shipTo.Fax = bodyItem.Fax;
            shipTo.Attn = bodyItem.Attn;
            shipTo.ShipVia = bodyItem.ShipVia;
            shipTo.TaxLocId = bodyItem.TaxLocId;
            shipTo.DistCode = bodyItem.DistCode;
            shipTo.Email = bodyItem.Email;
            shipTo.Internet = bodyItem.Internet;
            shipTo.PropertyChanged -= ShipTo_PropertyChanged;

            this.ValidateEntity(shipTo);
            return shipTo;
        }

        private void MarkToDelete(string shipToId)
        {
            ShipTo shipTo = this.Find(shipToId);
            if (shipTo == null)
                throw new NothingToProcessException(string.Format("Ship To ID '{0}' could not be found.", shipToId));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(ShipTo entity)
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

        private ShipTo Find(string shipToId)
        {
            var shipTo = Provider.Items?.Find(x => StringHelper.AreEqual(x.ShiptoId, shipToId, false));
            if (shipTo == null)
            {
                shipTo = EntityProvider.GetEntity<ShipTo, ShipToProvider>(new string[] { shipToId }, CompId, null);
                if (shipTo != null)
                    Provider.Items.Add(shipTo);
            }
            return shipTo;
        }
        #endregion Helper Methods

        #region Event Handler
        private void ShipTo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnShipTo_PropertyChanged(sender as ShipTo, e);
        }
        public virtual void OnShipTo_PropertyChanged(ShipTo entity, PropertyChangedEventArgs e)
        {

        }
        #endregion Event Handler

        #region Properties
        private ShipToProvider Provider { get; } = new ShipToProvider();

        private const string FunctionID = "9E350F53-2598-4A88-8531-7FDB58C5CB4B";
        #endregion Properties
    }
}
