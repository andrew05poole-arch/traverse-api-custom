#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
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
    public class ApiWMPickDetailController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "pick/{pickid}/detail/{id:int?}", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Get(string pickId = null, int? id = null)
        {            
            return Ok(await Load(pickId, id));
        }

        [ApiRoute(FunctionID, 2f, "pick/{pickid}/detail/{id:int?}", typeof(PickGenerated))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string pickId = null, int? id = null)
        {
            return Ok(await ProcessEditRequest(true, body, pickId, id));
        }

        [ApiRoute(FunctionID, 2f, "pick/{pickid}/detail/{id}", typeof(PickGenerated))]
        public async Task Delete(string pickId, int id)
        {
            await MarkToDelete(pickId, id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            this.EntityPropertyDictionary.Add(PickBase.Columns.ExtLocA.ToString(), ExtLocAPropertyChanged);
            this.EntityPropertyDictionary.Add(PickBase.Columns.ExtLocB.ToString(), ExtLocBPropertyChanged);
        }
        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Pick pick)
            {
                if (StringHelper.AreEqual(args.FieldName, "ExtLocA", false) || StringHelper.AreEqual(args.FieldName, "ExtLocB", false))
                {                   
                    if (args.ActualValue != null)
                    {
                        var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                        builder.AppendEquals(ExtLocationBase.Columns.Id, args.ActualValue.ToString());
                        builder.AppendEquals(ExtLocationBase.Columns.LocId, CurrentPickInfo.LocId);
                        args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].ExtLocId;
                    }
                }
            }
        }
        #endregion Overrides

        protected virtual async Task<EntityList<PickGenerated>> Load(string pickId, int? id)
        {
            if (CurrentPickInfo == null || CurrentPickInfo.Id != pickId)
            {
                var builder = new SqlFilterBuilder<PickInfoBase.Columns>();
                builder.AppendEquals(PickInfoBase.Columns.Id, pickId.ToString());
                CurrentPickInfo = (new PickInfoProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty)))[0];
                await this.FilterEntityListAsync(CurrentPickInfo.ReleasedPicksList);
            }

            if (!id.HasValue)
                Provider.Items.AddRange(CurrentPickInfo.ReleasedPicksList);
            else
            {
                Provider.Items.AddRange(CurrentPickInfo.ReleasedPicksList.FindAll(x => x.EntryNum == id));
                ProviderPick.Items.AddRange((CurrentPickInfo?.ReleasedPicksList?.Find(x => x.EntryNum == id))?.PickDetailList);
            }    
            
            return Provider.Items;
        }

        protected virtual async Task<PickGenerated> Find(string pickId, int id)
        {
            var list = await Load(pickId, id);          
            return list.Find(x => x.EntryNum == id);
        }

        protected virtual async Task<EntityList<PickGenerated>> ProcessEditRequest(bool isCreate, dynamic body, string pickId, int? id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && id != null)
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var entityList = new EntityList<PickGenerated>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, pickId, id);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            SavePickData(entityList);
            return entityList;
        }

        protected virtual async Task<PickGenerated> ProcessBodyItem(bool isCreate, dynamic bodyItem,string pickId, int? id)
        {
            int code = id.GetValueOrDefault();

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EntryNum) || bodyItem.EntryNum == null)
                bodyItem.EntryNum = code;
            else
                code = Convert.ToInt32(bodyItem.EntryNum);

            var entity = await this.Find(pickId, code);

            if (isCreate)
            {
                if (entity?.PickDetailList?.Find(x => x.EntryNum == code) != null)
                    return entity;               
            }
            
            if (entity == null)
                throw new InvalidValueException(string.Format("Pick Detail '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity,ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string pickId, int id)
        {
            var entity = await this.Find(pickId, id);

            if (entity == null)
                throw new InvalidValueException(string.Format("ID {0} could not be found on Pick ID '{1}'.", id, pickId));

            for(int i= entity.PickDetailList.Count; i > 0; i--)
                entity.PickDetailList.RemoveAt(i-1);

            this.ProviderPick.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (StringHelper.AreEqual(args.PropertyName, "PickDetailList"))
            {
                return this.CreatePick((PickGenerated)args.ParentObject, args.ItemModel);              
            }

            return null;
        }

        protected virtual Pick CreatePick(PickGenerated parent, dynamic bodyItem)
        {
            Pick entity = parent.PickDetailList.AddNew();
            entity.RecordPickStatus = PickStatus.New;
            entity.PickType = WMPickType.Pick;
            entity.SetDefaults();
            entity.SetPickDefaults();
            entity.QtyPicked = 1m;
            entity.MultipleSerialsYn = true;
            entity.ExtLocA = parent.ExtLocA;
            entity.LocId = parent.LocId;
            entity.ItemId = parent.ItemId;
            entity.TransId = parent.TransId;
            entity.SourceId = parent.SourceId;
            entity.EntryNum2 = parent.EntryNum2;
            entity.SeqNum = parent.SeqNum;
            entity.UOM = parent.UOM;
                       
            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = PickUpdateComplete;
            entity.PropertyChanged += Pick_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void PickUpdateComplete(object entityObject)
        {
            var entity = entityObject as Pick;
            entity.PropertyChanged -= Pick_PropertyChanged;          
        }

        protected virtual void SavePickData(EntityList<PickGenerated> pickGenereatedList)
        {
            foreach (PickGenerated pickGenerated in pickGenereatedList)
            {
                foreach (Pick pick in pickGenerated.PickDetailList)
                {
                    this.ValidatePickWorkingBin(pick);

                    if (this.IsValidToAppend(pickGenerated, pick))
                    {
                        if (pick.ValidateAll(true))
                        {
                            if (this.ValidateNegativeInventory(new EntityList<Pick>
                            {
                                pick
                            }))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            throw new InvalidValueException(pick.Error.ToString());
                        }
                    }
                }
            }

            foreach (PickGenerated pickGenerated in pickGenereatedList)
            {
                this.UpdatePickGen(pickGenerated);
            }
        }

        protected virtual void ValidatePickWorkingBin(Pick pick)
        {
            ExtLocation extLocation;
            this.ValidateWorkingBin(out extLocation, pick.LocId);
            pick.ExtLocATo = ((extLocation == null) ? null : new int?(extLocation.Id));
        }

        protected virtual void ValidateWorkingBin(out ExtLocation extLoc, string locId)
        {
            this.ValidateWorkingBin(out extLoc, locId, Utility.GetUserDefaultWorkingBin(this.CompId));
        }

        protected virtual void ValidateWorkingBin(out ExtLocation extLoc, string locId, string extLocId)
        {
            extLoc = null;

            if (string.IsNullOrEmpty(extLocId))
            {
                throw new Exception(string.IsNullOrEmpty(locId) ? "Not Defined Working Bin" : "Not Defined Working Bin Loc");
            }

            extLoc = ListHelper.GetExtLocationBin(extLocId, locId, this.CompId);

            if (extLoc == null)
            {
                throw new Exception(string.IsNullOrEmpty(locId) ? "Invalid Working Bin" : "Invalid Working Bin Loc");
            }
        }

        protected virtual void UpdatePickGen(PickGenerated pickGenerated)
        {
            EntityList<MoveQuantityHeader> moveQuantities = this.GetMoveQuantities(pickGenerated);
            string empty = string.Empty;
            
            if (!this.ValidateMoveQuantities(moveQuantities, out empty))
            {
                this.DisplayInvalidMovements(empty);
            }

            this.SaveQuantities(moveQuantities);
            this.SavePicks(pickGenerated.PickDetailList);
        }

        protected virtual void SavePicks(EntityList<Pick> pickDetailList)
        {
            if (pickDetailList.IsDirty)
            {
                PickProvider pickProvider = new PickProvider();
                pickProvider.Items.AddRange(pickDetailList);
                pickProvider.Items.DeletedItems.AddRange(pickDetailList.DeletedItems);
                pickProvider.Update(this.CompId);
                pickDetailList.DeletedItems.Clear();
            }
        }

        protected virtual void SaveQuantities(EntityList<MoveQuantityHeader> moveQuantityList)
        {
            if (ListBase<MoveQuantityHeader>.IsNullOrEmpty(moveQuantityList))
            {
                return;
            }

            using (TransactionManager transactionManager = new TransactionManager(this.CompId))
            {
                try
                {
                    if (moveQuantityList.IsDirty)
                    {
                        transactionManager.BeginTransaction();
                        MoveQuantityHeaderProvider moveQuantityHeaderProvider = new MoveQuantityHeaderProvider();
                        moveQuantityHeaderProvider.TransMan = transactionManager;
                        moveQuantityHeaderProvider.Items.AddRange(moveQuantityList);
                        moveQuantityHeaderProvider.Items.DeletedItems.AddRange(moveQuantityList.DeletedItems);
                        moveQuantityHeaderProvider.Update(this.CompId);
                        moveQuantityList.DeletedItems.Clear();

                        if (!moveQuantityList.IsDirty)
                        {
                            MoveQuantityPost moveQuantityPost = TransactionPostBase.LoadPostingEngine<MoveQuantityPost>(this.CompId);
                            moveQuantityPost.TransMan = transactionManager;
                            moveQuantityPost.TransactionList.AddRange(moveQuantityList);

                            if (moveQuantityPost.CheckClosedPeriod())
                            {
                                moveQuantityPost.SetDefaults();
                                moveQuantityPost.Execute(null);
                            }
                        }

                        transactionManager.Commit();
                    }
                }
                catch (Exception ex)
                {
                    if (transactionManager != null)
                    {
                        transactionManager.Rollback();
                    }

                    throw new Exception(string.Format(Localization.GetLocalizedString("InvalidPickProcess"), WMPickType.Pick.ToString(), Environment.NewLine, ex.Message));
                }
            }
        }

        protected virtual bool ValidateMoveQuantities(EntityList<MoveQuantityHeader> list, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!ListBase<MoveQuantityHeader>.IsNullOrEmpty(list))
            {
                foreach (MoveQuantityHeader moveQuantityHeader in list)
                {
                    if (!moveQuantityHeader.ValidateAll(true))
                    {
                        errorMessage = moveQuantityHeader.Error;
                    }
                }
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        protected virtual void DisplayInvalidMovements(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return;
            }

            throw new Exception(string.Format(Localization.GetLocalizedString("InvalidPickProcess"), WMPickType.Pick.ToString(), Environment.NewLine, errorMessage));
        }

        protected virtual EntityList<MoveQuantityHeader> GetMoveQuantities(PickGenerated pickGenerated)
        {
            EntityList<MoveQuantityHeader> entityList = new EntityList<MoveQuantityHeader>();
            if (pickGenerated != null)
            {
                foreach (Pick pick in pickGenerated.PickDetailList.FindAll((Pick x) => x.IsNew || x.IsDeleted))
                {
                    MoveQuantityHeader moveQuantityHeader = this.MovePickQuantities(pick);

                    if (moveQuantityHeader != null)
                    {
                        entityList.Add(moveQuantityHeader);
                    }
                }

                foreach (Pick pick2 in pickGenerated.PickDetailList.DeletedItems)
                {
                    MoveQuantityHeader moveQuantityHeader = this.MovePickQuantities(pick2);

                    if (moveQuantityHeader != null)
                    {
                        entityList.Add(moveQuantityHeader);
                    }
                }
            }

            return entityList;
        }

        protected virtual MoveQuantityHeader MovePickQuantities(Pick pick)
        {
            if (pick != null && pick.InItem != null && pick.InItem.IsQuantityTracked)
            {
                int? num = pick.ExtLocA;
                int? num2 = pick.ExtLocATo;

                if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null))
                {
                    num2 = pick.ExtLocB;
                    num = pick.ExtLocBTo;

                    if (num2.GetValueOrDefault() == num.GetValueOrDefault() & num2 != null == (num != null))
                    {
                        return null;
                    }
                }

                return null;
            }

            return null;
        }

        protected virtual bool IsValidToAppend(PickGenerated header, Pick pick)
        {
            return pick != null && this.IsValidToAppend(header, pick.QtyPicked);
        }

        protected virtual bool IsValidToAppend(PickGenerated header, decimal qtyPicked)
        {
            if (header == null)
            {
                return false;
            }

            decimal? pickOveragePct = Utility.GetPickOveragePct(header.SourceType, header.LocId, this.CompId);

            if (pickOveragePct != null)
            {
                if (pickOveragePct.GetValueOrDefault(0m) == 0m && qtyPicked > header.QtyReq - (header.QtyPicked + header.QtyPicking))
                {
                    throw new Exception("Qty Pick more than required");
                }

                if (pickOveragePct.GetValueOrDefault(0m) > 0m && qtyPicked > Utility.GetQtyAllowed(header.QtyReq, pickOveragePct.GetValueOrDefault(0m)) - (header.QtyPicked + header.QtyPicking))
                {
                    throw new Exception("Qty Pick more than overage");
                }
            }

            return true;
        }

        protected virtual bool ValidateNegativeInventory(EntityList<Pick> picks)
        {
            return ListBase<Pick>.IsNullOrEmpty(picks) || picks.Count(new Func<Pick, bool>(this.FindNegativePicks)) <= 0 || this.ValidateNegativeInventory(Utility.GetNegativeInventory(this.CompId), picks.FindAll(new Predicate<Pick>(this.FindNegativePicks)));
        }

        protected virtual bool FindNegativePicks(Pick pick)
        {
            return pick != null && pick.IsNew && pick.InItem != null && pick.InItem.IsQuantityTracked && pick.QtyPickedBase > 0m && pick.PickType == WMPickType.Pick && pick.RecordPickStatus == PickStatus.New;
        }

        protected virtual bool ValidateNegativeInventory(byte negativeInvOpt, EntityList<Pick> picks)
        {
            if (ListBase<Pick>.IsNullOrEmpty(picks) || negativeInvOpt == 0)
            {
                return true;
            }

            using (NegativeInventoryProcess negativeInventoryProcess = this.InitNegativeInventoryProcess())
            {
                if (negativeInventoryProcess != null)
                {
                    negativeInventoryProcess.NegativeList.AddRange(picks);
                    negativeInventoryProcess.Execute(null);
                }
            }

            return true;
        }

        protected virtual NegativeInventoryProcess InitNegativeInventoryProcess()
        {
            return ProcessBase.LoadProcessEngine<NegativeInventoryProcess>(this.CompId);
        }

        #region Body Item Update Methods
        protected virtual void ExtLocAPropertyChanged(dynamic body, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Pick)
            {
                if (body.ExtLocA != null)
                {
                    var builder = new SqlFilterBuilder<ExtLocationBase.Columns>();
                    
                    if (StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocA.ToString(), false))
                        builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, body.ExtLocA);
                    
                    builder.AppendEquals(ExtLocationBase.Columns.LocId, CurrentPickInfo.LocId);
                    args.ActualValue = ((EntityProvider.GetEntityList<ExtLocationBin, ExtLocationBinProvider>(this.CompId, new FilterCriteria(builder.ToString(), null), null)))[0].Id;
                }
            }
        }

        protected virtual void ExtLocBPropertyChanged(dynamic body, ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is Pick)
            {
                if (body.ExtLocB != null)
                {
                    var builder = new SqlFilterBuilder<ExtLocationBase.Columns>(); ;
                    if (StringHelper.AreEqual(args.FieldName, PickBase.Columns.ExtLocB.ToString(), false))
                        builder.AppendEquals(ExtLocationBase.Columns.ExtLocId, body.ExtLocB);
                    
                    builder.AppendEquals(ExtLocationBase.Columns.LocId, CurrentPickInfo.LocId);
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
            Action<PickGenerated> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PickGenerated);
        }

        private void Pick_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Pick> action = null;
            if (PickPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Pick);
        }
        #endregion Event Handlers

        #region Properties
        protected PickInfo CurrentPickInfo { get; set; }
        protected PickGeneratedProvider Provider { get; } = new PickGeneratedProvider();
        protected PickProvider ProviderPick { get; } = new PickProvider();
        protected SortedDictionary<string, Action<Pick>> PickPropertyDictionary { get; } = new SortedDictionary<string, Action<Pick>>();
        protected SortedDictionary<string, Action<PickGenerated>> PropertyDictionary { get; } = new SortedDictionary<string, Action<PickGenerated>>();
        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        public const string FunctionID = "c877ffee-fdf2-4f75-9835-0b43a0dc3658";
        #endregion Fields
    }
}
