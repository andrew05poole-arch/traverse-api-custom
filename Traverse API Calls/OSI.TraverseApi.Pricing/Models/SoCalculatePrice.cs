#region Using Directives
using System;
using System.ComponentModel;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.Pricing;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.SalesOrder
{
    public class SoCalculatePrice : PriceEngine
    {
        #region Constructors
        public SoCalculatePrice(string compId)
            :base(compId)
        {
            Quantity = 1M;
            ExchRate = 1;
            Date = DateTime.Today;
            CurrencyId = ConfigurationValue.GetRule<string>(AppId.SM, ConfigurationValue.BaseCurrency, this.CompanyId);
        }
        #endregion Constructors

        #region Methods
        public override decimal CalculatePrice()
        {
            var response = base.CalculatePrice();
            PopulateResponseFields();
            return response;
        }

        private void SetCustomerDefault()
        {
            if (string.IsNullOrWhiteSpace(CustomerId))
                return;

            var customer = EntityProvider.GetEntity<Customer, CustomerProvider>(new string[] { CustomerId }, this.CompanyId, null);
            if (customer == null)
                return;

            if (string.IsNullOrEmpty(CurrencyId))
                CurrencyId = customer.CurrencyId;

            if (string.IsNullOrEmpty(CustomerLevel))
                CustomerLevel = customer.CustLevel;
        }

        private void SetItemDefault()
        {
            if (string.IsNullOrEmpty(ItemId))
                return;

            if (Item == null)
                return;

            if (string.IsNullOrEmpty(LocationId))
                LocationId = Item.AllLocations[0].LocId;

            if (string.IsNullOrEmpty(Uom))
                Uom = Item.UomBase;

            if (string.IsNullOrEmpty(PriceId))
                PriceId = Item.PriceId;
        }

        private void SetLocation()
        {
            if (Item != null)
                Item.CurrentLocation = string.IsNullOrEmpty(LocationId) ? null : Item.GetLocationById(LocationId);
        }

        private void PopulateResponseFields()
        {
            if (Item != null)
            {
                ExtCost = Item.GetSaleCost(Quantity, Uom) * ExchRate;
                UnitCost = ExtCost / Quantity;
            }

            if (this.CalculatedPriceStatus == CalculatedPriceStatus.Valid
                && this.ParameterStatus == ParameterStatus.AllValid)
            {
                if (IsBaseCurrency)
                    CalculatedPrice = Rounding.Round(CalculatedPrice * Convert.ToDecimal(ExchRate), Rounding.RoundingType.UnitPrice, CurrencyId, this.CompanyId);

                CalcExtPrice = CalculatedPrice * Quantity;
                GrossProfitMargin = CalcExtPrice - ExtCost;
            }
            else if (CalculatedPriceStatus == CalculatedPriceStatus.Invalid)
            {
                switch (ParameterStatus)
                {
                    case ParameterStatus.InvalidItemId:
                        throw new InvalidValueException("Item ID is invalid.");
                    case ParameterStatus.InvalidLocId:
                        throw new InvalidValueException("Location is invalid.");
                    case ParameterStatus.InvalidUom:
                        throw new InvalidValueException("Unit is invalid.");
                }
            }
        }
        #endregion Methods

        #region Properties
        [Bindable(true)]
        public override string ItemId 
        {
            get => base.ItemId;
            set
            {
                base.ItemId = value;
                SetItemDefault();
            }
        }

        [Bindable(true)]
        public override string LocationId 
        {
            get => base.LocationId;
            set
            {
                base.LocationId = value;
                SetLocation();
            }
        }

        public override string CustomerId 
        { 
            get => base.CustomerId;
            set
            {
                base.CustomerId = value;
                SetCustomerDefault();
            }
        }

        [Bindable(true)]
        public decimal ExchRate { get; set; }

        [Bindable(true)]
        public decimal UnitCost { get; private set; }

        [Bindable(true)]
        public decimal CalcExtPrice { get; private set; }

        [Bindable(true)]
        public decimal ExtCost { get; private set; }

        [Bindable(true)]
        public decimal GrossProfitMargin { get; private set; }

        [Bindable(true)]
        public string LocationStatus
        {
            get
            {
                if (Item != null && Item.CurrentLocation != null)
                    return Item.CurrentLocation.InventoryStatus.ToString();

                return null;
            }
        }
        #endregion Properties
    }
}
