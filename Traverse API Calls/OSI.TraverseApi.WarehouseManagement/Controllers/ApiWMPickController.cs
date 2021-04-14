#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class ApiWMPickController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "pick/{id?}", typeof(PickInfo))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "pick/{id}/finished", typeof(PickInfo))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ExecuteFinishedPick(body, id));
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates(){}
        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is PickInfo)
            {
                if(StringHelper.AreEqual(args.FieldName,"PackingBin",false) && PackingBin != null)
                    args.ActualValue = PackingBin[0]?.ExtLocId;

                if (StringHelper.AreEqual(args.FieldName, "Status", false))
                    args.ActualValue = (byte)((PickInfo)args.Entity).Status;

                if (StringHelper.AreEqual(args.FieldName, "HostId", false))
                    args.ActualValue = Workstation;

                if (StringHelper.AreEqual(args.FieldName, "UId", false))
                    args.ActualValue = UserPick;
            }

            if (args.Entity is Pick)
            {
                if(StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocA.ToString(), false) || StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocB.ToString(), false))
                {
                    if (args.ActualValue != null)
                    {
                        var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                        builder.AppendEquals(ExtLocationBase.Columns.Id, args.ActualValue.ToString());
                        builder.AppendEquals(ExtLocationBase.Columns.LocId, ((Pick)args.Entity).LocId);
                        args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].ExtLocId;
                    }
                }
            }
        }
        #endregion Overrides

        protected virtual async Task<EntityList<PickInfo>> Load(string id)
        {
            var builder = new SqlFilterBuilder<PickInfoBase.Columns>();

            if (string.IsNullOrEmpty(id))
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
            else
            {
                builder.AppendEquals(PickInfoBase.Columns.Id, id.ToString());
                CurrentPickInfo = (new PickInfoProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty)))[0];
                Provider.Items.Add(CurrentPickInfo);
            }
            await this.FilterEntityListAsync(Provider.Items);

            return Provider.Items;
        }

        protected virtual async Task<PickInfo> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.Id, id, false));
        }

        protected virtual async Task<PickInfo> ExecuteFinishedPick(dynamic body, string id)
        {
            var entity = await this.Find(id);

            if(entity != null)
            {
                this.DisposeProcess();
                InitializeProcessEngine(body);

                if (body != null)
                {
                    if(!ApiUserSkipped.IsApiUserSkipped(body[0].PackingBin))
                    {
                        var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                        builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, body[0].PackingBin);
                        builder.AppendEquals(ExtLocationBase.Columns.LocId, entity.LocId);
                        PackingBin = (EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(),null), null));
                        this.ProcessEngine.AddPackingBinByLoc(entity.LocId, PackingBin[0].Id);
                    }
                    else
                        this.ProcessEngine.MoveToPacking = true;
                }
                else
                    this.ProcessEngine.MoveToPacking = true;

                this.ProcessEngine.Execute(null);
            }

            Provider.Items.Clear();
            entity = await this.Find(id);
            return entity;
        }

        protected virtual void InitializeProcessEngine(dynamic body)
        {
            Workstation = ApiUserSkipped.IsApiUserSkipped(body[0].HostId) ? ApplicationContext.SessionId : body[0].HostId;
            UserPick = ApiUserSkipped.IsApiUserSkipped(body[0].UId) ? ApplicationContext.CurrentUser : body[0].UId;

            if (this.CurrentPickInfo != null)
            {
                foreach (PickGenerated pickGenerated in CurrentPickInfo.ReleasedPicksList)
                {
                    EntityList<Pick> entityList = new EntityList<Pick>();

                    EntityList<Pick> pickDetailListByUser = PickDetailListByUser(pickGenerated, body);

                    if (pickDetailListByUser.FindAll((Pick x) => StringHelper.AreEqual(x.LocId, this.CurrentPickInfo.LocId)).Count > 0)
                    {
                        entityList.AddRange(pickGenerated.PickDetailList.FindAll((Pick x) => StringHelper.AreEqual(x.LocId, this.CurrentPickInfo.LocId)).ToArray());
                        this.ProcessEngine.PicksToProcess.AddRange(entityList);
                    }
                }
            }

            this.ProcessEngine.UId = UserPick;
            this.ProcessEngine.HostId = Workstation;
            this.ProcessEngine.FunctionId = new Guid(FunctionID);
        }

        public virtual EntityList<Pick> PickDetailListByUser(PickGenerated pickGenerated, dynamic body)
        {
            return pickGenerated.PickDetailList.FindAll((Pick x) => StringHelper.AreEqual(x.UId, UserPick) && StringHelper.AreEqual(x.HostId, Workstation));
        }

        protected virtual void DisposeProcess()
        {
            if (this._finishProcess != null)
            {
                this._finishProcess.Dispose();
                this._finishProcess = null;
            }
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
            var entity = sender as PickInfo;
            entity.PropertyChanged -= Entity_PropertyChanged;
            Action<PickInfo> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(entity);
            entity.PropertyChanged += Entity_PropertyChanged;
        }
        #endregion Event Handlers

        #region Properties
        protected PickInfo CurrentPickInfo { get; set; }
        protected PickInfoProvider Provider { get; } = new PickInfoProvider();
        protected SortedDictionary<string, Action<PickInfo>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PickInfo>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        protected virtual FinishPickProcess ProcessEngine
        {
            get
            {
                if (this._finishProcess == null)
                {
                    this._finishProcess = ProcessBase.LoadProcessEngine<FinishPickProcess>(this.CompId);
                }
                return this._finishProcess;
            }
        }
        protected EntityList<ExtLocationBin> PackingBin { get; set; }
        protected string Workstation { get; set; }
        protected string UserPick { get; set; }

        #endregion Properties

        #region Fields
        public const string FunctionID = "4cc14ee6-f646-46a9-8d2c-ce2f71aaa5d3";
        private FinishPickProcess _finishProcess;
        #endregion Fields
    }
}
