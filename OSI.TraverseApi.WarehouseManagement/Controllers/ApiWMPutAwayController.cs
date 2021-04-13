#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WM;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMPutAwayController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "putaway/location/{locationid}/receivingbin/{id}", typeof(PutAway))]
        public async Task<IHttpActionResult> Get(string locationid = null, string id = null) 
        {
            return Ok(await Load(locationid, id));
        }
                
        [ApiRoute(FunctionID, 2f, "putaway/{locationid}/write", typeof(PutAway))]
        public async Task Write([FromBody] dynamic body, string locationId = null)
        {
            Ok( this.ProcessWriteRequest(locationId));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates(){}

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            if (args.Entity is MoveQuantityHeader move)
            {
                if (StringHelper.AreEqual(args.FieldName, "MoveById", false))
                {
                    args.ActualValue = move.InItem?.ItemId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMBinTo", false))
                {
                    args.ActualValue = move.ExtLocAToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerTo", false))
                {
                    args.ActualValue = move.ExtLocBToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerFrom", false))
                {
                    args.ActualValue = move.ExtLocBFromId;
                }
            }
            if (args.Entity is MoveQuantitySerial moveSer)
            {
                if (StringHelper.AreEqual(args.FieldName, "MoveById", false))
                {
                    args.ActualValue = moveSer.InItem?.ItemId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMBinTo", false))
                {
                    args.ActualValue = moveSer.ExtLocAToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerTo", false))
                {
                    args.ActualValue = moveSer.ExtLocBToId;
                }
                if (StringHelper.AreEqual(args.FieldName, "WMContainerFrom", false))
                {
                    args.ActualValue = moveSer.ExtLocBFromId;
                }
            }
        }
        #endregion

        protected virtual async Task<EntityList<PutAway>> Load(string locationId, string bin)
        {
            if (Provider.Items.Count <= 0 || ((!string.IsNullOrEmpty(locationId) && !string.IsNullOrEmpty(bin)) &&
                !Provider.Items.Exists(i => StringHelper.AreEqual(locationId, i.LocId, false) && StringHelper.AreEqual(locationId, i.BinInfo?.ExtLocId, false))))
            {
                int binId = 0;
                ExtLocationBinProvider binProvider = new ExtLocationBinProvider();
                SqlFilterBuilder<ExtLocationBin.Columns> binBuilder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                binBuilder.AppendEquals(ExtLocationBase.Columns.ExtLocId.ToString(), bin);
                binProvider.Load(this.CompId, new FilterCriteria(binBuilder.ToString(), string.Empty));
                if (binProvider.Items != null && binProvider.Items.Count > 0)
                    binId = binProvider.Items[0].Id;

                if (binId == 0)
                    throw new Exception(string.Format("Bin {0} could not be found on Location {1}",bin,locationId));
                                
                this.Provider.Items.AddRange( PutAwayProvider.GetEntityList(locationId, binId, ApplicationContext.CurrentUser, ApplicationContext.SessionId, PutAwayScreenFunctionId, this.CompId));

                await this.FilterEntityListAsync(this.Provider.Items);
            }

            return Provider.Items;
        }
                
        protected virtual string ProcessWriteRequest(string locationId)
        {
            if (this.ProcessMoveQuantityPost(locationId))
            {
                return "Write Data Completed succesfully";
            }
            return "Nothing to process";
        }
        
        protected virtual bool ProcessMoveQuantityPost(string locationId)
        {            
            if ( this.PostingEngine.CheckClosedPeriod())
            {
                this.PostingEngine.PostRun = ProcessBase.GenerateProcessId();
                this.PostingEngine.ResetLists();
                this.PostingEngine.SetDefaults();
                this.PostingEngine.Comments = string.Empty;
                SqlFilterBuilder<MoveQuantityBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<MoveQuantityBase.Columns>();
                sqlFilterBuilder.AppendEquals(MoveQuantityBase.Columns.FunctionId, PutAwayScreenFunctionId);
                sqlFilterBuilder.AppendEquals(MoveQuantityBase.Columns.UId, TRAVERSE.Core.ApplicationContext.CurrentUser);
                sqlFilterBuilder.AppendEquals(MoveQuantityBase.Columns.HostId, TRAVERSE.Core.ApplicationContext.SessionId);
                sqlFilterBuilder.AppendEquals(MoveQuantityBase.Columns.LocId, locationId);
                EntityList<MoveQuantityHeader> list = new MoveQuantityHeaderProvider().Load(this.CompId, new FilterCriteria(sqlFilterBuilder.ToString(), string.Empty));
                if (!ListBase<MoveQuantityHeader>.IsNullOrEmpty(list))
                {
                    this.PostingEngine.TransactionList.AddRange(list);
                }
                if (this.PostingEngine.ValidateProperties())
                {
                    this.PostingEngine.Execute(null);
                }
                else
                {
                    this.DisplayInvalidProperties();
                }
                return true;
            }
            return false;
        }

        protected virtual void DisplayInvalidProperties()
        {
            if (this.PostingEngine is ProcessActivityBase)
            {
                ProcessActivityBase process = this.PostingEngine as ProcessActivityBase;
                StringBuilder stringBuilder = new StringBuilder();
                foreach (EntityProperty<string> current in process.InvalidPropertyList)
                {
                    stringBuilder.AppendLine(string.Format("{0}", current.Value));
                }
                if (stringBuilder.Length > 0)
                {
                    throw new Exception(stringBuilder.ToString());
                }
            }
        }

        protected virtual PutAwayPost CreateNewPostingEngine()
        {
            if (this._engine == null)
            {
                this._engine = TransactionPostBase.LoadPostingEngine<PutAwayPost>(this.CompId);
            }
            return this._engine;
        }

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
            Action<PutAway> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PutAway);
        }
        #endregion  Event Handlers

        #region Properties
        protected PutAwayProvider Provider { get; } = new PutAwayProvider();

        protected virtual MoveQuantityPost PostingEngine
        {
            get
            {
                return this.CreateNewPostingEngine();
            }
        }

        protected SortedDictionary<string, Action<PutAway>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PutAway>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public PutAwayPost _engine;

        public const string FunctionID = "80bfffe4-1438-4ede-b7a2-5bf00ab2f7e0";

        public const string PutAwayScreenFunctionId = "204e3c3e-a936-4aff-a65b-9cac0df9a411";
        #endregion Fields
    }
}