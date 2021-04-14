#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Business.WM;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMReceiveHeaderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "receive/{documentno}/detail/{id:int?}", typeof(ReceiptWMS))]
        public async Task<IHttpActionResult> Get(string documentNo = null, int? id = null)
        {
            return Ok(await this.Load(documentNo, id));
        }

        [ApiRoute(FunctionID, 2f, "receive/{documentno}/detail/{id:int?}", typeof(ReceiptWMS))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string documentNo = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, documentNo, id));
        }

        [ApiRoute(FunctionID, 2f, "receive/{documentno}/confirm", typeof(ReceiptWMS))]
        public async Task<IHttpActionResult> Confirm([FromBody] dynamic body, string documentNo = null)
        {
            return Ok(await ProcessConfirmRequest(body, documentNo));
        }

        [ApiRoute(FunctionID, 2f, "receive/{documentno}/detail/{id:int}", typeof(ReceiptWMS))]
        public async Task Delete(string documentNo, int id)
        {
            await this.MarkToDelete(documentNo, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() 
        {
            //bodyItem
            this.EntityPropertyDictionary.Add(ReceiptBase.Columns.ExtLocA.ToString(), BinPropertyChanged);
            this.EntityPropertyDictionary.Add(ReceiptBase.Columns.ExtLocB.ToString(), ContainerPropertyChanged);
            
            //Receipt
            this.ReceiptPropertyDictionary.Add(ReceiptBase.Columns.Qty.ToString(),
               (entity) =>
               {
                   bool? isSerial = entity.InItem.InventoryType == InventoryType.Serial ? false : true;

                   if (!(bool)isSerial)
                       entity.Qty = 1;
               });
            this.ReceiptPropertyDictionary.Add(ReceiptBase.Columns.SerNum.ToString(), 
                (entity) =>
                {
                    if (entity.SerNum != null)
                    {
                        if ((CurrentReceiptWMS.ReceiptList?.FindAll(x => StringHelper.AreEqual(x.SerNum, entity.SerNum, false))).Count > 1)
                            throw new Exception(string.Format("Serial Number '{0}' already exists for this item id '{1}'", entity.SerNum, entity.ItemId));
                    }
                });
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Receipt )
            {
                if (StringHelper.AreEqual(args.FieldName, "ExtLocA", false) || StringHelper.AreEqual(args.FieldName, "ExtLocB", false))
                {
                    if (args.ActualValue != null)
                    {
                        var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                        builder.AppendEquals(ExtLocationBase.Columns.Id, args.ActualValue.ToString());
                        builder.AppendEquals(ExtLocationBase.Columns.LocId, CurrentDocument.LocId);
                        args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].ExtLocId;
                    }
                }
            }
        }
        #endregion Overrides

        protected virtual async Task<EntityList<ReceiptWMS>> Load(string documentNo, int? id)
        {
            FindDocumentHeader(documentNo);

            if(CurrentDocument.Source != null)
            {
                DataSet dataSet = await Task.Run(() => Receipt.FindOrders(this.CompId, documentNo, null, new List<ReceiptSource>
                {
                    (ReceiptSource)CurrentDocument.Source
                }));

                DataTable dataTable = dataSet.Tables.OfType<DataTable>().FirstOrDefault<DataTable>();
                this.RecalculateLocId(dataTable);

                ReceiptWMSList = LoadEntityList(dataTable);
            }

            if (id.HasValue)
            {
                ReceiptProvider.Items.AddRange((ReceiptWMSList?.Find(x => x.RcptKey == id) as ReceiptWMS)?.ReceiptList);
                return ReceiptWMSList.FindAll(x => x.RcptKey == id);
            }
            else
                return ReceiptWMSList;
        }

        protected virtual async Task<ReceiptWMS> Find(string documentNo, int id)
        {
            var list = await Load(documentNo, id);
            CurrentReceiptWMS = list.Find(x => x.RcptKey == id);
            return CurrentReceiptWMS;
        }

        protected virtual async Task<EntityList<ReceiptWMS>> ProcessEditRequest(bool isCreate, dynamic body, string documentNo, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new EntityList<ReceiptWMS>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, documentNo, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            SaveReceiptData(entityList);
            return entityList;
        }

        protected virtual async Task<ReceiptWMS> ProcessBodyItem(bool isCreate, dynamic bodyItem, string documentNo, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.RcptKey) || bodyItem.RcptKey == null)
                bodyItem.RcptKey = code;
            else
                code = Convert.ToInt32(bodyItem.RcptKey);

            var entity = await this.Find(documentNo, code);

            if (isCreate)
            {
                if (entity?.ReceiptList?.Find(x => x.RcptKey == code) != null)
                    return entity;
            }

            if (entity == null)
                throw new InvalidValueException(string.Format("Receive Detail '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (StringHelper.AreEqual(args.PropertyName, "ReceiptList"))
            {
                return this.CreateReceipt((ReceiptWMS)args.ParentObject, args.ItemModel);
            }

            return null;
        }

        protected virtual Receipt CreateReceipt(ReceiptWMS parent, dynamic bodyItem)
        {
            Receipt entity = parent.ReceiptList.AddNew();
            entity.MultipleSerialsYn = true;
            entity.ItemId = parent.ItemId;
            entity.Qty = 1m;
            entity.LocId = parent.LocId;
            entity.ReceiptNum = parent.ReceiptNum;
            entity.SourceType = (ReceiptSource)parent.Source;
            entity.ExtLocA = DfltBin();
            entity.TransId = parent.RcpTransId;
            entity.EntryNum2 = parent.EntryNum2;
            entity.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = ReceiptUpdateComplete;
            entity.PropertyChanged += Receipt_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void ReceiptUpdateComplete(object entityObject)
        {
            var entity = entityObject as Receipt;
            entity.PropertyChanged -= Receipt_PropertyChanged;
        }

        protected virtual int? DfltBin()
        {
            ExtLocationBin extLocationBin = ListHelper.GetExtLocationBin(Utility.GetUserDefaultReceivingBin(this.CompId), CurrentDocument.LocId, this.CompId);
            if (extLocationBin == null)
            {
                extLocationBin = ListHelper.GetExtLocationBin(Utility.GetLocationDefaultReceivingBin(this.CompId, CurrentDocument.LocId), CurrentDocument.LocId, this.CompId);
            }
            if (extLocationBin != null)
            {
                return new int?(extLocationBin.Id);
            }
            return null;
        }

        protected virtual  EntityList<ReceiptWMS> LoadEntityList(DataTable dataTable)
        {
            EntityList<ReceiptWMS> entityList = new EntityList<ReceiptWMS>();

            foreach (object obj in dataTable.Rows)
            {
                DataRow dataRow = (DataRow)obj;
                ReceiptWMS receiptWMS = new ReceiptWMS(this.CompId);
                entityList.Add(receiptWMS);
                receiptWMS.SuppressEntityEvents = true;
                receiptWMS.ItemId = dataRow["ItemID"].ToString();
                receiptWMS.LocId = dataRow["LocID"].ToString();
                receiptWMS.LotNum = ((!string.IsNullOrEmpty(dataRow["LotNumber"].ToString())) ? dataRow["LotNumber"].ToString() : null);
                receiptWMS.UOM = dataRow["Units"].ToString();
                receiptWMS.Source = Convert.ToByte(dataRow["Source"].ToString());
                receiptWMS.TransId = dataRow["TransID"].ToString();
                receiptWMS.EntryNum2 = new long?((receiptWMS.SourceType == ReceiptSource.MPProduction) ? Convert.ToInt64(dataRow["MPTransID"].ToString()) : Convert.ToInt64(dataRow["EntryNum"].ToString()));
                receiptWMS.RcpEntryNum = new long?(Convert.ToInt64(dataRow["EntryNum"].ToString()));
                receiptWMS.QtyReq = Convert.ToDecimal(dataRow["QtyOrdered"]);
                receiptWMS.QtyRcvd = Convert.ToDecimal(dataRow["QtyReceived"]);
                receiptWMS.UPCCode = dataRow["Upccode"].ToString();
                if (!string.IsNullOrEmpty(dataRow["RequiredDate"].ToString()))
                {
                    receiptWMS.RequiredDate = new DateTime?(Convert.ToDateTime(dataRow["RequiredDate"]));
                }
                if (!string.IsNullOrEmpty(dataRow["ReqShipDate"].ToString()))
                {
                    receiptWMS.ReqShipDate = new DateTime?(Convert.ToDateTime(dataRow["ReqShipDate"]));
                }
                receiptWMS.VendorPartNo = dataRow["VendorPartNumber"].ToString();
                receiptWMS.RcptKey = entityList.Count;
                receiptWMS.LinkSeqNum = dataRow.Field<int?>("LinkSeqNum");
                receiptWMS.LinkTransId = dataRow.Field<string>("LinkTransId");
                receiptWMS.BOLNum = dataRow.Field<string>("BOLNum");
                receiptWMS.Notes = dataRow.Field<string>("Notes");
                receiptWMS.ReceiptNum = CurrentDocument.ReceiptNum;
                LoadReceipts(receiptWMS);
                receiptWMS.AcceptChanges();
                receiptWMS.SuppressEntityEvents = false;
                receiptWMS.ParentCollection = entityList;
            }
            entityList.AllowRemove = false;
            entityList.Sort("EntryNum2, RequiredDate");
            return entityList;
        }

        protected virtual void FindDocumentHeader(string documentNo)
        {
            CurrentDocument = new DocumentHeader();

            var definition = EntityProvider.GetLookupDef("WMRecptDocAll");
            var headerList = definition.GetDataSet(this.CompId, null).Tables[0];

            foreach(DataRow row in headerList.Rows)
            {
                var dict = row.Table.Columns
                             .Cast<DataColumn>()
                             .ToDictionary(c => c.ColumnName, c => row[c]);

                object value = null;
                var val = dict.TryGetValue("OrderNo", out value);

                if(StringHelper.AreEqual(documentNo, value.ToString(), false))
                {
                    this.CurrentDocument.Id = Convert.ToString(row["Id"]);
                    this.CurrentDocument.DocumentNo = Convert.ToString(row["OrderNo"]);
                    this.CurrentDocument.Source = new int?(Convert.ToInt32(row["Source"]));
                    this.CurrentDocument.ReceiptNum = ProcessBase.GenerateProcessId();
                    return;
                }
            }          
        }

        protected virtual void RecalculateLocId(DataTable table)
        {
            if (table != null && table.Rows.Count > 0)
            {
                IEnumerable<IGrouping<string, DataRow>> source = from x in table.Rows.OfType<DataRow>()
                                                                 group x by Convert.ToString(x[LocationBase.Columns.LocId.ToString()]);
                if (source.FirstOrDefault((IGrouping<string, DataRow> x) => StringHelper.AreEqual(x.Key, CurrentDocument.LocId) && x.Count<DataRow>() > 0) == null)
                {
                    IGrouping<string, DataRow> grouping = source.FirstOrDefault((IGrouping<string, DataRow> x) => x.Count<DataRow>() > 0 && !string.IsNullOrEmpty(x.Key));
                    if (grouping != null)
                    {
                        this.CurrentDocument.LocId = grouping.Key;
                    }
                }
            }
        }

        protected virtual async Task<EntityList<ReceiptWMS>> ProcessConfirmRequest(dynamic body, string documentNo)
        {
            try
            {
                Workstation = ApiUserSkipped.IsApiUserSkipped(body[0].HostId) ? ApplicationContext.SessionId : body[0].HostId;
                UserReceipt = ApiUserSkipped.IsApiUserSkipped(body[0].UId) ? ApplicationContext.CurrentUser : body[0].UId;
                EntityList<ReceiptWMS> headerList = await Load(documentNo, null);

                this.DisposeProcess();
                List<string> list = new List<string>();
                if (!ListBase<ReceiptWMS>.IsNullOrEmpty(headerList))
                {
                    list = (from s in headerList.FindAll((ReceiptWMS h) => h.QtyReceiving > 0m)
                            select s.RcpTransId).Distinct<string>().ToList<string>();
                }

                SqlFilterBuilder<ReceiptBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<ReceiptBase.Columns>();
                sqlFilterBuilder.AppendIn(ReceiptBase.Columns.TransId, string.Join(",", list));
                sqlFilterBuilder.AppendEquals(ReceiptBase.Columns.LocId, CurrentDocument.LocId);
                sqlFilterBuilder.AppendEquals(ReceiptBase.Columns.UId, UserReceipt);
                sqlFilterBuilder.AppendEquals(ReceiptBase.Columns.HostId, Workstation);
                sqlFilterBuilder.AppendIn(ReceiptBase.Columns.Status, string.Format("{0}, {1}", ReceiptStatus.New.ToString("D"), ReceiptStatus.Confirmed.ToString("D")));
                ReceiptProvider provider = new ReceiptProvider();
                provider.Load(this.CompId, new FilterCriteria(sqlFilterBuilder.ToString(), null));
                if (provider.Count > 0 && list.Count > 0)
                {
                    if ((ReceiptSource)CurrentDocument.Source == ReceiptSource.MPProduction)
                    {
                        List<int> collection = this.FillProductionOrders();
                        this.ProcessEngine.ChangeStatusTransIdList.AddRange(collection);
                    }
                    this.ProcessEngine.ReptSource = (ReceiptSource)CurrentDocument.Source;
                    this.ProcessEngine.Comments = string.Empty;
                    this.ProcessEngine.RcptList.Clear();
                    this.ProcessEngine.RcptList.AddRange(provider.Items);
                    this.ProcessEngine.UserId = UserReceipt;
                    this.ProcessEngine.HostId = Workstation;

                    if (!this.ProcessEngine.ValidateProperties())
                    {
                        this.DisplayInvalidProperties();
                    }

                    this.ProcessEngine.Execute(null);                
                }
                else
                {
                    throw new InvalidValueException("There is nothing to confirm.");
                }

                return headerList;                
            }
            catch (Exception ex)
            {
                throw new InvalidValueException(ex.Message);
            }
        }

        protected virtual void DisposeProcess()
        {
            if (this._rcptConfirm != null)
            {
                this._rcptConfirm.Dispose();
                this._rcptConfirm = null;
            }
        }

        protected virtual void LoadReceipts(ReceiptWMS receiptWMS)
        {
            if (receiptWMS.ReceiptList.Count <= 0 )
            {
                SqlFilterBuilder<ReceiptBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<ReceiptBase.Columns>();
                sqlFilterBuilder.AppendIn(ReceiptBase.Columns.TransId, receiptWMS.RcpTransId);
                if (!string.IsNullOrEmpty(receiptWMS.LocId))
                {
                    sqlFilterBuilder.AppendEquals(ReceiptBase.Columns.LocId, receiptWMS.LocId);
                }
                sqlFilterBuilder.AppendEquals(ReceiptBase.Columns.Source, receiptWMS.Source.ToString());
                sqlFilterBuilder.AppendEquals(ReceiptBase.Columns.EntryNum, receiptWMS.EntryNum2.Value.ToString());
                receiptWMS.ReceiptList.AddRange(EntityProvider.GetEntityList<Receipt, ReceiptProvider>(this.CompId, new FilterCriteria(sqlFilterBuilder.ToString(), string.Empty), receiptWMS.TransMan));
            }
        }

        public virtual void DisplayInvalidProperties()
        {
            if (this.Process is ProcessActivityBase)
            {
                ProcessActivityBase processActivityBase = this.Process as ProcessActivityBase;
                StringBuilder stringBuilder = new StringBuilder();
                foreach (EntityProperty<string> entityProperty in processActivityBase.InvalidPropertyList)
                {
                    stringBuilder.AppendLine(string.Format("{0}", entityProperty.Value));
                }
                if (stringBuilder.Length > 0)
                {
                    throw new InvalidValueException(stringBuilder.ToString());
                }
            }
        }

        protected virtual List<int> FillProductionOrders()
        {
            List<int> list = new List<int>();
            try
            {
                DataTable wmProductionConfirmList = Receipt.GetWmProductionConfirmList(this.CompId);

                if (wmProductionConfirmList != null)
                {
                    SQLStringBuilder sqlstringBuilder = new SQLStringBuilder
                    {
                        UnicodeCompatible = false
                    };
                    sqlstringBuilder.AppendEquals("OrderNo", CurrentDocument.DocumentNo);
                    sqlstringBuilder.AppendEquals("LocID", CurrentDocument.LocId);
                    DataView defaultView = wmProductionConfirmList.DefaultView;
                    defaultView.RowFilter = sqlstringBuilder.ToString();
                    foreach (object obj in defaultView.ToTable().Rows)
                    {
                        DataRow dataRow = (DataRow)obj;
                        if (Convert.ToBoolean(dataRow["Completed"]))
                        {
                            list.Add(Convert.ToInt32(dataRow["TransId"]));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidValueException(ex.Message);
            }
            return list;
        }

        protected virtual async Task MarkToDelete(string documentNo, int id)
        {
            var entity = await this.Find(documentNo, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("ID {0} could not be found on Document ID '{1}'.", id, documentNo));

            for (int i = entity.ReceiptList.Count; i > 0; i--)
            {
                if(entity.ReceiptList[i-1].Status != 2)
                    entity.ReceiptList.RemoveAt(i - 1);
            }
                
            this.ReceiptProvider.Update(this.CompId);
        }

        protected virtual void SaveReceiptData(EntityList<ReceiptWMS> ReceiptGenereatedList)
        {
            foreach (ReceiptWMS receiptGenerated in ReceiptGenereatedList)
            {
                foreach (Receipt receipt in receiptGenerated.ReceiptList)
                {

                    if (this.IsValidToAppend(receiptGenerated, receipt, receipt.Qty))
                    {
                        if (receipt.ValidateAll(true))
                            receiptGenerated.LabelQty = Convert.ToInt32(receiptGenerated.QtyReceiving);
                        else
                            throw new Exception(receipt.Error.ToString());
                    }
                }
            }

            foreach (ReceiptWMS receiptGenerated in ReceiptGenereatedList)
            {
                this.UpdateReceipt(receiptGenerated);
            }
        }

        public virtual void UpdateReceipt(ReceiptWMS receiptGenerated)
        {
            ReceiptProvider receiptProvider = new ReceiptProvider();
            receiptProvider.TransMan = receiptGenerated.TransMan;
            receiptProvider.Items.AddRange(receiptGenerated.ReceiptList);
            receiptProvider.Items.DeletedItems.AddRange(receiptGenerated.ReceiptList.DeletedItems);
            if (receiptGenerated.InItem != null && receiptGenerated.InItem.InventoryType == InventoryType.Serial && receiptGenerated.ReceiptList.DeletedItems.Count > 0)
            {
                receiptGenerated.LabelQty = 1;
            }
            receiptProvider.Update(this.CompId);
            receiptGenerated.ReceiptList.DeletedItems.Clear();
        }

        protected virtual bool IsValidToAppend(ReceiptWMS header, Receipt receipt, decimal qtyReceived)
        {
            if (header == null || receipt == null)
            {
                return false;
            }
            decimal? receiveOveragePct = Utility.GetReceiveOveragePct(header.SourceType, header.LocId, this.CompId);
            if (receiveOveragePct != null)
            {
                if (receiveOveragePct.GetValueOrDefault(0m) == 0m && qtyReceived > header.QtyReq - (header.QtyRcvd + header.QtyReceiving))
                {
                    throw new Exception("Quantity More Than Required Item.");
                }
                if (receiveOveragePct.GetValueOrDefault(0m) > 0m && qtyReceived > Utility.GetQtyAllowed(header.QtyReq, receiveOveragePct.GetValueOrDefault(0m)) - (header.QtyRcvd + header.QtyReceiving))
                {
                    throw new Exception("Quantity More Than Overage Item.");
                }
            }
            return true;
        }

        #region Body Item Update Methods
        protected virtual void BinPropertyChanged(dynamic body, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Receipt receipt)
            {
                if (body.ExtLocA != null)
                {
                    var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();

                    if (StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocA.ToString(), false))
                        builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, body.ExtLocA);

                    builder.AppendEquals(ExtLocationBase.Columns.LocId, CurrentDocument.LocId);
                    args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].Id;
                }
            }
        }

        protected virtual void ContainerPropertyChanged(dynamic body, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Receipt receipt)
            {
                if (body.ExtLocB != null)
                {
                    var builder = new SqlFilterBuilder<ExtLocationBase.Columns>(); ;
                    if (StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocB.ToString(), false))
                        builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, body.ExtLocB);

                    builder.AppendEquals(ExtLocationBase.Columns.LocId, CurrentDocument.LocId);
                    args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].Id;
                }
            }
        }
        #endregion Body Item Update Methods
        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<ReceiptWMS> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ReceiptWMS);
        }

        private void Receipt_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Receipt> action = null;
            if (ReceiptPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Receipt);
        }
        #endregion Event Handlers

        #region Properties
        ReceiptProvider ReceiptProvider { get; } = new ReceiptProvider();
        protected SortedDictionary<string, Action<ReceiptWMS>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ReceiptWMS>>();
        protected SortedDictionary<string, Action<Receipt>> ReceiptPropertyDictionary { get; } = new SortedDictionary<string, Action<Receipt>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        protected virtual WMSRcptConfirmProcess ProcessEngine
        {
            get
            {
                if (this._rcptConfirm == null)
                {
                    this._rcptConfirm = ProcessBase.LoadProcessEngine<WMSRcptConfirmProcess>(this.CompId);
                }
                return this._rcptConfirm;
            }
        }
        protected virtual ProcessBase Process
        {
            get
            {
                return this._process;
            }
            set
            {
                this._process = value;
            }
        }
        protected DocumentHeader CurrentDocument{get;set; }
        protected EntityList<ReceiptWMS> ReceiptWMSList { get; set; } = new EntityList<ReceiptWMS>();
        protected ReceiptWMS CurrentReceiptWMS { get; set; }
        protected string Workstation { get; set; }
        protected string UserReceipt { get; set; }
        #endregion Properties

        #region Fields
        public const string FunctionID = "b6fc751e-24ee-44be-9187-64ee781e389a";
        private WMSRcptConfirmProcess _rcptConfirm;
        private ProcessBase _process;
        #endregion Fields
    }
}
