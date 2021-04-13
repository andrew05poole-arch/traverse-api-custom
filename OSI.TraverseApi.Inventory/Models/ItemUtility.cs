using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRAVERSE.Business;
using Comp = TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;

namespace OSI.TraverseApi.Inventory
{
    public class ItemUtility
    {
        public static decimal GetExchRate(Item entity)
        {
            if (entity.CurrentLocation == null)
                return 0M;

            return GetExchRate(entity.CurrentLocation);
        }

        public static decimal GetExchRate(ItemLocation entity)
        {
            var currency = ConfigurationValue.GetRule<string>(AppId.SM, ConfigurationValue.BaseCurrency, entity.CompId);
            if (string.Compare(currency, entity.CurrencyIdACV, StringComparison.InvariantCultureIgnoreCase) == 0)
                return 1M;

            var period = Comp.PeriodConversion.GetFiscalPeriod(DateTime.Today, entity.CompId);
            if (period != null)
            {
                DataSet set = 
                    EntityProvider.ExecuteCommand(string.Format(
                        Properties.Resources.GetExchangeRateSql, 
                        period.FiscalYear, period.FiscalPeriod,
                        SqlUtil.Encode(currency, true),
                        SqlUtil.Encode(entity.CurrencyIdACV, true)),
                        entity.CompId, null);

                if (set != null && set.Tables.Count > 0 && set.Tables[0].Rows.Count > 0)
                {
                    return (Convert.IsDBNull(set.Tables[0].Rows[0]["Rate"])  || set.Tables[0].Rows[0]["Rate"] == null) ? 0M : 
                        Convert.ToDecimal(set.Tables[0].Rows[0]["Rate"]);
                }
                else
                    return 0M;
            }
            return 1M;
        }

        /// <summary>
        /// Calculate ACV Values for Item Location Cost Buckets
        /// </summary>
        /// <param name="location">This the item location to process</param>
        /// <param name="field">This is which field to calculate.
        /// 0 = All fields; 1 = Cost Avg; 2 = Cost Base; 3 = Cost Landed Last; 4 = Cost Last; 5 = Cost Std
        /// </param>
        public static void CalculateACVValues(ItemLocation location, byte field)
        {
            decimal exchRate = GetExchRate(location.ParentEntity as Item);
            if (field == 0 || field == 1)
                location.CostAvgACV = location.CostAvg.Value * exchRate;

            if (field == 0 || field == 2)
                location.CostBaseACV = location.CostBase.Value * exchRate;

            if (field == 0 || field == 3)
                location.CostLandedLastACV = location.CostLandedLast.Value * exchRate;

            if (field == 0 || field == 4)
                location.CostLastACV = location.CostLast.Value * exchRate;

            if (field == 0 || field == 5)
                location.CostStdACV = location.CostStd.Value * exchRate;
        }
    }
}
