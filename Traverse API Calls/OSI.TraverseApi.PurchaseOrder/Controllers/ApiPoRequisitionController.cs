#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.CompanySetup;
using TRAVERSE.Business.PurchaseOrder;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.PurchaseOrder.Controllers
{
    public class ApiPoRequisitionController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "requisition/{reqid?}", typeof(Requisition))]
        public IHttpActionResult Get(int? reqId = null)
        {
            return Ok(Load(reqId));
        }

        [ApiRoute(FunctionID, 2f, "requisition/{reqid?}", typeof(Requisition))]
        public IHttpActionResult Put([FromBody] dynamic body, int? reqId = null)
        {
            List<Requisition> requisitionList = new List<Requisition>();
            object[] list;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && reqId != null)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var requisition = UpdateRequisition(item, reqId);
                if (!requisitionList.Contains(requisition))
                    requisitionList.Add(requisition);
            }
            this.Provider?.Update(this.CompId);

            return Ok(requisitionList);
        }

        [ApiRoute(FunctionID, 2f, "requisition", typeof(Requisition))]
        public IHttpActionResult Add([FromBody] dynamic body)
        {
            List<Requisition> requisitionList = new List<Requisition>();
            object[] list;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            foreach (dynamic item in list)
            {
                var requisition = CreateRequisition(item);
                this.Provider.Items.Add(requisition);

                if (!requisitionList.Contains(requisition))
                    requisitionList.Add(requisition);
            }
            this.Provider?.Update(this.CompId);

            return Ok(requisitionList);
        }

        [ApiRoute(FunctionID, 2f, "requisition/{reqid}", typeof(Requisition))]
        public IHttpActionResult Delete(int? reqId = null)
        {
            this.MarkToDelete(reqId);
            this.Provider?.Update(this.CompId);

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        private EntityList<Requisition> Load(int? reqId)
        {
            SqlFilterBuilder<RequisitionBase.Columns> builder = new SqlFilterBuilder<RequisitionBase.Columns>();
            if (reqId != null)
                builder.AppendEquals(RequisitionBase.Columns.ReqId, reqId.ToString());
            Provider.CompId = CompId;
            Provider.SetPage(PageNumber, PageSize);
            Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

            if (Provider.Items.Count <= 0)
            {
                if (reqId != null)
                    throw new InvalidValueException(string.Format("Requisition ID '{0}' could not be found.", reqId));
                else
                    throw new InvalidValueException("No results were found.");
            }
            return Provider.Items;
        }

        private Requisition UpdateRequisition(dynamic bodyItem, int? reqId)
        {
            Requisition requisition = this.Find((int?)bodyItem.ReqId ?? reqId);
            if (requisition == null)
                throw new InvalidValueException(string.Format("Requisition ID '{0}' could not be found.", bodyItem.ReqId ?? reqId));

            requisition.PropertyChanged += Requisition_PropertyChanged;
            requisition.VendorId = bodyItem.VendorId ?? requisition.VendorId;
            requisition.ItemId = bodyItem.ItemId ?? requisition.ItemId;           
            requisition.CurrencyId = bodyItem.CurrencyId ?? requisition.CurrencyId;          
            requisition.LocId = bodyItem.LocId ?? requisition.LocId;
            requisition.Description = bodyItem.Description ?? requisition.Description;
            requisition.Qty = (decimal?)bodyItem.Qty ?? requisition.Qty;          
            requisition.Uom = bodyItem.Uom ?? requisition.Uom;
            requisition.InitDate = (DateTime?)bodyItem.InitDate ?? requisition.InitDate;
            requisition.SourceApp = bodyItem.SourceApp ?? requisition.SourceApp;
            requisition.RefId = bodyItem.RefId ?? requisition.RefId;
            requisition.GenerateYn = (bool?)bodyItem.GenerateYn ?? requisition.GenerateYn;
            requisition.GlAcct = bodyItem.GlAcct ?? requisition.GlAcct;
            requisition.DropShipYn = (bool?)bodyItem.DropShipYn ?? requisition.DropShipYn;
            requisition.ExpReceiptDate = (DateTime?)bodyItem.ExpReceiptDate ?? requisition.ExpReceiptDate;
            requisition.AddnlDescr = bodyItem.AddnlDescr ?? requisition.AddnlDescr;
            requisition.UnitCost = (decimal?)bodyItem.UnitCost ?? requisition.UnitCost;
            requisition.ExtCost = (decimal?)bodyItem.ExtCost ?? requisition.ExtCost;
            requisition.PropertyChanged -= Requisition_PropertyChanged;

            this.ValidateEntity(requisition);

            return requisition;
        }       

        private Requisition CreateRequisition(dynamic bodyItem)
        {
            Requisition requisition = this.Find(bodyItem.ReqId );
            if (requisition != null)
                throw new InvalidValueException(string.Format("Requisition ID '{0}' already exists.", bodyItem.DistCode));
            else
                requisition = new Requisition(this.CompId);

            requisition.PropertyChanged += Requisition_PropertyChanged;
            requisition.VendorId = bodyItem.VendorId;
            requisition.ItemId = bodyItem.ItemId;
            requisition.SetDefaults();
            requisition.SetItemDefaults();
            requisition.CurrencyId = bodyItem.CurrencyId ?? requisition.CurrencyId;
            requisition.LocId = bodyItem.LocId ?? requisition.LocId;
            requisition.Description = bodyItem.Description ?? requisition.Description;
            requisition.Qty = (decimal?)bodyItem.Qty ?? requisition.Qty;
            Item item = requisition.SMItemInfo;           
            requisition.Uom = bodyItem.Uom ?? requisition.Uom;
            requisition.InitDate = (DateTime?)bodyItem.InitDate ?? requisition.InitDate;
            requisition.EnteredBy = bodyItem.EnteredBy ?? requisition.EnteredBy;
            requisition.SourceApp = bodyItem.SourceApp ?? requisition.SourceApp;
            requisition.RefId = bodyItem.RefId;
            requisition.GenerateYn = (bool?)bodyItem.GenerateYn ?? requisition.GenerateYn;
            requisition.GlAcct = bodyItem.GlAcct ?? requisition.GlAcct;
            requisition.DropShipYn = (bool?)bodyItem.DropShipYn ?? requisition.DropShipYn;
            requisition.ExpReceiptDate = (DateTime?)bodyItem.ExpReceiptDate;
            requisition.AddnlDescr = bodyItem.AddnlDescr;
            requisition.UnitCost = (decimal?)bodyItem.UnitCost ?? item.UnitCost;
            requisition.ExtCost = (decimal?)bodyItem.ExtCost ?? requisition.ExtCost;
            requisition.PropertyChanged -= Requisition_PropertyChanged;

            this.ValidateEntity(requisition);

            return requisition;
        }

        private void MarkToDelete(int? reqId)
        {
            Requisition requisition = this.Find(reqId);
            if (requisition == null)
                throw new NothingToProcessException(string.Format("Requisition ID '{0}' could not be found.", reqId));
            else
                this.Provider.Items[0].MarkToDelete();
        }

        private void ValidateEntity(Requisition entity)
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

        private Requisition Find(int? reqId)
        {
            var requisition = Provider.Items?.Find(x => x.ReqId == reqId);
            if (requisition == null)
            {
                requisition = EntityProvider.GetEntity<Requisition, RequisitionProvider>(new string[] { reqId.ToString() }, CompId, null);
                if (requisition != null)
                    Provider.Items.Add(requisition);
            }
            return requisition;
        }
        #endregion Helper Methods

        #region Event Handler
        private void Requisition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnRequisition_PropertyChanged(sender as Requisition, e);
        }
        public virtual void OnRequisition_PropertyChanged(Requisition entity, PropertyChangedEventArgs e)
        {
            if (entity != null)
            {
                if (e.PropertyName == RequisitionBase.Columns.ItemId.ToString())
                {
                    if (entity.ItemId != null)
                    {
                        entity.SetItemDefaults();
                        return;
                    }
                }
                else
                {
                    if (e.PropertyName == RequisitionBase.Columns.Uom.ToString())
                    {
                        entity.SetCostDefaults();
                        return;
                    }
                    if (e.PropertyName == RequisitionBase.Columns.LocId.ToString())
                    {
                        entity.SetItemLocationDefaults();
                        return;
                    }
                    if (e.PropertyName == RequisitionBase.Columns.Qty.ToString())
                    {
                        entity.SetCostDefaults();
                        entity.CalculateExtendedCost();
                        return;
                    }
                    if (e.PropertyName == RequisitionBase.Columns.UnitCost.ToString())
                    {
                        entity.CalculateExtendedCost();
                        return;
                    }
                    if (e.PropertyName == RequisitionBase.Columns.ExtCost.ToString())
                    {
                        entity.CalculateUnitCost();
                        return;
                    }
                    if (e.PropertyName == RequisitionBase.Columns.VendorId.ToString())
                    {
                        if (entity.VendorInfo != null)
                        {
                            entity.SetVendorDefaults();
                        }
                        else
                        {
                            entity.CurrencyId = Utility.BaseCurrencyId;
                        }
                        return;
                    }
                }
            }
        }
        #endregion Event Handler

        #region Properties
        private RequisitionProvider Provider { get; } = new RequisitionProvider();

        private const string FunctionID = "F9B9EE5D-B001-468C-9C84-B13224FA4AE9";
        #endregion Properties
    }
}
