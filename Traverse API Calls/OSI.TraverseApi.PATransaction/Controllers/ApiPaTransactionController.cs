#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaTransactionController: ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(Transaction))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(Transaction))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id?}", typeof(Transaction))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "transaction/{id}", typeof(Transaction))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods

        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //Earning Property Changes
            EarnPropertyDictionary.Add(TransEarnBase.Columns.Amount.ToString(), (entity) => entity.Rate = entity.RecalcRate());
            EarnPropertyDictionary.Add(TransEarnBase.Columns.Rate.ToString(), (entity) => entity.Amount = entity.RecalcAmount());
            EarnPropertyDictionary.Add(TransEarnBase.Columns.Hours.ToString(), (entity) => entity.Amount = entity.RecalcAmount());
            EarnPropertyDictionary.Add(TransEarnBase.Columns.EarningCode.ToString(), (entity) => 
            {
                entity.Rate = entity.GetDefaultRate();
                entity.Amount = entity.Rate * entity.Hours;
            });
            EarnPropertyDictionary.Add(TransEarnBase.Columns.TaxGroupId.ToString(), (entity) => entity.ResetTaxCodes());
        }
        #endregion

        protected virtual async Task<EntityList<Transaction>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.EmployeeId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    this.Provider.Load(this.CompId);
                else
                {
                    var builder = new SqlFilterBuilder<TransEarnBase.Columns>();
                    builder.AppendEquals(TransEarnBase.Columns.EmployeeId, id);
                    var list = new TransactionProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Transaction> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.EmployeeId, id, false));
        }

        protected virtual async Task<List<Transaction>> ProcessEditRequest(bool isCreate, dynamic body, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Transaction ID is provided along with more than one record.");

            var entityList = new List<Transaction>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);

            foreach (var pair in ProcessList)
                pair.Value.Invoke(pair.Key);

            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Transaction> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EmployeeId) || bodyItem.EmployeeId == null)
                bodyItem.EmployeeId = code;
            else
                code = bodyItem.EmployeeId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                entity = new Transaction(this.CompId, id);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Employee ID {0} could not be found.", code, code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Employee '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (StringHelper.AreEqual(args.PropertyName, "TransEarnList"))
            {
                if (((Transaction)args.ParentObject).IsNew)
                    return this.CreateEarning((Transaction)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateEarning((Transaction)args.ParentObject, args.ItemModel);

            }
            else if (StringHelper.AreEqual(args.PropertyName, "TransDeductList"))
            {

                if (((Transaction)args.ParentObject).IsNew)
                    return this.CreateDeduction((Transaction)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateDeductions((Transaction)args.ParentObject, args.ItemModel);
            }
            else if(StringHelper.AreEqual(args.PropertyName, "TransCostList"))
            {

                if (((Transaction)args.ParentObject).IsNew)
                    return this.CreateEmpCost((Transaction)args.ParentObject, args.ItemModel);
                else
                    return this.UpdateEmpCost((Transaction)args.ParentObject, args.ItemModel);
            }

            return null;
        }

        #region Earnings Update Methods
        protected virtual TransEarn UpdateEarning(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EmployeeId))
                throw new InvalidValueException("Employee ID is required.");

            this.FilterEntityList(parent.TransEarnList, ApiPaTransactionEarnController.FunctionID);
            TransEarn entity = (parent.TransEarnList as EntityList<TransEarn>).Find(TransEarnBase.Columns.Id, (int)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Earn ID {0} could not be found on Employee ID '{1}'.", bodyItem.Id, parent.EmployeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = EarningUpdateComplete;
            entity.PropertyChanged += Earning_PropertyChanged;
            
            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransEarn CreateEarning(Transaction parent, dynamic bodyItem)
        {
            TransEarn entity = parent.TransEarnList.AddNew();

            entity.SetDefaultValues();
            entity.PaYear = PayrollContext.PayrollYear;
            entity.Rate = entity.GetDefaultRate();
            entity.PostedYn = false;

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = EarningUpdateComplete;
            entity.PropertyChanged += Earning_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void EarningUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransEarn;
            entity.PropertyChanged -= Earning_PropertyChanged;
        }
        #endregion Earnings Update Methods

        #region Deductions Update Methods
        protected virtual TransDeduct UpdateDeductions(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EmployeeId))
                throw new InvalidValueException("Employee ID is required.");

            this.FilterEntityList(parent.TransDeductList, ApiPaTransactionEmployeeDeductionController.FunctionID);
            TransDeduct entity = (parent.TransDeductList as EntityList<TransDeduct>).Find(TransDeductBase.Columns.Id, (int)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Deduction ID {0} could not be found on Employee ID '{1}'.", bodyItem.Id, parent.EmployeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DeductionUpdateComplete;
            entity.PropertyChanged += Deduction_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransDeduct CreateDeduction(Transaction parent, dynamic bodyItem)
        {
            TransDeduct entity = parent.TransDeductList.AddNew();
            entity.SetDefaultValues();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = DeductionUpdateComplete;
            entity.PropertyChanged += Deduction_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void DeductionUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransDeduct;
            entity.PropertyChanged -= Deduction_PropertyChanged;
        }
        #endregion Deductions Update Methods

        #region Employer Costs Update Methods
        protected virtual TransCost UpdateEmpCost(Transaction parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EmployeeId))
                throw new InvalidValueException("Employee ID is required.");

            this.FilterEntityList(parent.TransCostList, ApiPaTransactionEmployerCostController.FunctionID);
            TransCost entity = (parent.TransCostList as EntityList<TransCost>).Find(TransCostBase.Columns.Id, (int)bodyItem.Id);
            if (entity == null)
                throw new InvalidValueException(string.Format("Transaction Deduction ID {0} could not be found on Employee ID '{1}'.", bodyItem.Id, parent.EmployeeId));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = EmployerCostsUpdateComplete;
            entity.PropertyChanged += EmployerCost_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual TransCost CreateEmpCost(Transaction parent, dynamic bodyItem)
        {
            TransCost entity = parent.TransCostList.AddNew();
            entity.SetDefaultValues();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            ((ApiEntityModel)bodyItem).FieldUpdateIsComplete = EmployerCostsUpdateComplete;
            entity.PropertyChanged += EmployerCost_PropertyChanged;

            Request.RegisterForDispose(entity);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return entity;
        }

        protected virtual void EmployerCostsUpdateComplete(object entityObject)
        {
            var entity = entityObject as TransCost;
            entity.PropertyChanged -= EmployerCost_PropertyChanged;
        }
        #endregion Employer Costs Update Methods

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
            Action<Transaction> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Transaction);
        }

        private void Earning_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransEarn> action = null;
            if (EarnPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransEarn);
        }

        private void Deduction_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransDeduct> action = null;
            if (DeductPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransDeduct);
        }

        private void EmployerCost_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<TransCost> action = null;
            if (EmployerCostPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as TransCost);
        }
        #endregion Event Handlers

        #region Properties
        protected TransactionProvider Provider { get; } = new TransactionProvider();

        protected Dictionary<Transaction, Action<Transaction>> ProcessList { get; } = new Dictionary<Transaction, Action<Transaction>>();

        protected SortedDictionary<string, Action<Transaction>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Transaction>>();

        protected SortedDictionary<string, Action<TransEarn>> EarnPropertyDictionary { get; } = new SortedDictionary<string, Action<TransEarn>>();

        protected SortedDictionary<string, Action<TransDeduct>> DeductPropertyDictionary { get; } = new SortedDictionary<string, Action<TransDeduct>>();

        protected SortedDictionary<string, Action<TransCost>> EmployerCostPropertyDictionary { get; } = new SortedDictionary<string, Action<TransCost>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion

        #region Fields
        public const string FunctionID = "6dea7340-33c5-4ff7-931b-91c8fddcc715";
        #endregion
    }
}
