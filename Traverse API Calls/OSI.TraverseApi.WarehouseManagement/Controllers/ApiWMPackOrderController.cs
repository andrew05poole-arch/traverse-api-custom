#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WM;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.WarehouseManagement.Controllers
{
    public class ApiWMPackOrderController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "pack/{orderno?}", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Get(string orderNo = null)
        {
            return Ok(await this.Load(orderNo));
        }

        [ApiRoute(FunctionID, 2f, "pack/{orderno}/confirm", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo = null)
        {
            return Ok(await ExecuteConfirmPackOrder(orderNo));
        }
        #endregion Web Methods

        #region Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add(PickGenerated.Columns.ExtLocA.ToString(), BinPropertyChanged);
        }

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            base.ProcessCustomResponse(args);
            var extLocation = new ExtLocationBin();

            if ((args.Entity as PickGenerated)?.ExtLocA != null)
                extLocation = EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, null, null)?.Find(x => x.Id == (args.Entity as PickGenerated)?.ExtLocA);

            if (args.FieldName == "ExtLocA")
                args.ActualValue = extLocation.ExtLocId;
        }
        #endregion

        protected virtual async Task<EntityList<PickGenerated>> Load(string orderNo)
        {
            var builder = new SqlFilterBuilder<PickGeneratedBase.Columns>();

            if (string.IsNullOrEmpty(orderNo))
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
            else
            {
                builder.AppendEquals(PickGeneratedBase.Columns.TransId, orderNo.ToString());
                CurrentPick = (new PickGeneratedProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty)))[0];
                Provider.Items.Add(CurrentPick);
            }
            await this.FilterEntityListAsync(Provider.Items);

            return Provider.Items;
        }

        protected virtual async Task<PickGenerated> Find(string orderNo)
        {
            var list = await Load(orderNo);
            return list.Find(x => StringHelper.AreEqual(x.TransId, orderNo, false));
        }

        protected virtual async Task<PickGenerated> ExecuteConfirmPackOrder(string orderNo)
        {
            var entity = await this.Find(orderNo);

            if (entity != null)
            {
                InitializeProcessEngine(orderNo);
                this.ProcessEngine.Execute(null);
            }

            Provider.Items.Clear();
            return entity;
        }

        protected virtual void InitializeProcessEngine(string orderNo)
        {
            this.DisposeProcess();
            this.ProcessEngine.UserId = TRAVERSE.Core.ApplicationContext.CurrentUser;
            this.ProcessEngine.HostId = TRAVERSE.Core.ApplicationContext.SessionId;
            if (CurrentPick != null)
            {
                string text = this.BuildPickFilter(this.GetCurrentPicksToProcess());
                PickProvider pickProvider = new PickProvider();
                if (pickProvider.Load(this.CompId, new FilterCriteria(text, null)).Count > 0)
                {
                    this.ProcessEngine.PickList.AddRange(pickProvider.Items);
                    this.ProcessEngine.PickFilter = text;
                }
            }
        }

        protected virtual string BuildPickFilter(EntityList<Pick> pickList)
        {
            if (ListBase<Pick>.IsNullOrEmpty(pickList))
            {
                return string.Empty;
            }
            SqlFilterBuilder<PickBase.Columns> sqlFilterBuilder = new SqlFilterBuilder<PickBase.Columns>();
            sqlFilterBuilder.AppendEquals(PickBase.Columns.Type, WMPickType.Pack.ToString("D"));
            sqlFilterBuilder.AppendNotEquals(PickBase.Columns.Status, PickStatus.Completed.ToString("D"));
            sqlFilterBuilder.AppendEquals(PickBase.Columns.UId, this.ProcessEngine.UserId);
            sqlFilterBuilder.AppendEquals(PickBase.Columns.HostId, this.ProcessEngine.HostId);
            sqlFilterBuilder.AppendEquals(PickBase.Columns.LocId, CurrentPick.LocId);

            if (CurrentPick.ExtLocA.HasValue)
            {
                sqlFilterBuilder.AppendEquals(PickBase.Columns.ExtLocA, CurrentPick.ExtLocA.Value.ToString());
            }
            else
            {
                sqlFilterBuilder.AppendIsNull(PickBase.Columns.ExtLocA);
            }
            SqlFilterBuilder<PickBase.Columns> pick = sqlFilterBuilder;
            return sqlFilterBuilder.ToString();
        }

        protected virtual EntityList<Pick> GetCurrentPicksToProcess()
        {
            EntityList<Pick> entityList = new EntityList<Pick>();
            if (!ListBase<PickGenerated>.IsNullOrEmpty(this.PickGenList))
            {
                foreach (PickGenerated current in this.PickGenList)
                {
                    if (!ListBase<Pick>.IsNullOrEmpty(current.PickDetailListByUser))
                    {
                        foreach (Pick current2 in current.PickDetailListByUser)
                        {
                            if (this.IsValidPackPick(current2, CurrentPick.LocId, CurrentPick.ExtLocA) || current2.RecordPickStatus == PickStatus.Completed)
                            {
                                entityList.Add(current2);
                            }
                        }
                    }
                }
            }
            return entityList;
        }

        protected virtual bool IsValidPackPick(Pick pick, string locId, int? extLocA)
        {
            if (pick != null && StringHelper.AreEqual(pick.LocId, locId))
            {
                int? extLocA2 = pick.ExtLocA;
                int? num = extLocA;
                if (extLocA2.GetValueOrDefault() == num.GetValueOrDefault() & extLocA2.HasValue == num.HasValue)
                {
                    return pick.PickType == WMPickType.Pack;
                }
            }
            return false;
        }

        protected virtual void DisposeProcess()
        {
            if (this._confirmProcess != null)
            {
                this._confirmProcess.Dispose();
                this._confirmProcess = null;
            }
        }

        protected virtual void BinPropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Pick pickGenerated)
            {
                args.ActualValue = (EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, null, null)?.Find(x => StringHelper.AreEqual(x.ExtLocId, bodyItem.packing_bin, false) && x.LocId == bodyItem.LocId) as ExtLocationBin).Id;
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
            Action<PickGenerated> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PickGenerated);
        }
        #endregion Event Handlers

        #region Properties
        protected PickGenerated CurrentPick { get; set; }
        protected PickGeneratedProvider Provider { get; } = new PickGeneratedProvider();
        protected SortedDictionary<string, Action<PickGenerated>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PickGenerated>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        protected virtual PickConfirmProcess ProcessEngine
        {
            get
            {
                if (this._confirmProcess == null)
                {
                    this._confirmProcess = ProcessBase.LoadProcessEngine<PickConfirmProcess>(this.CompId);
                }
                return this._confirmProcess;
            }
        }

        protected EntityList<PickGenerated> PickGenList { get; set; } = new EntityList<PickGenerated>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "89e5a049-4dfc-4277-83d3-32c941bf977b";
        private PickConfirmProcess _confirmProcess;
        #endregion Fields
    }
}

