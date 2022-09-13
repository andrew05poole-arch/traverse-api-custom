#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Payroll;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.Payroll.Controllers
{
    public class ApiPaEmployeeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "employee/{id?}", typeof(Employee))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await Load(id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{id?}", typeof(Employee))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{id?}", typeof(Employee))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "employee/{id}", typeof(Employee))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates()
        {
            //bodyItem
            this.EntityPropertyDictionary.Add("DedCodeId", this.CodePropertyChanged);
            //Employee Detail
            this.EmployeeDetailPropertyDictionary.Add(EmployeeDetail.Columns.Salary.ToString(), this.SalaryPropertyChanged);
            this.EmployeeDetailPropertyDictionary.Add(EmployeeDetail.Columns.HourlyRate.ToString(), this.HourlyRateChanged);
            this.EmployeeDetailPropertyDictionary.Add(EmployeeDetail.Columns.TaxGroupId.ToString(), this.TaxGroupIdChanged);
            //Employee Deductions
            this.EmployeeDeductionsPropertyDictionary.Add(DeductEmployee.Columns.DeductionCodeId.ToString(), this.DeductionCodeIdPropertyChanded);
            //Employeer Deductions - Employer Cost
            this.EmployerDeductionsPropertyDictionary.Add(DeductEmployer.Columns.DeductionCodeId.ToString(), this.DeductionCodeIdPropertyChanded);
            //Employee Pay Distributions - Direct Deposit
            this.EmployeePayDistributionPropertyDictionary.Add(PayDistributionBase.Columns.AccountNumber.ToString(), this.AccountNumberPropertyChanged);
            this.EmployeePayDistributionPropertyDictionary.Add(PayDistributionBase.Columns.AmountPercent.ToString(), this.AmountPercentPropertyChanged);
            //Employee Valid Earning Codes 
            this.EmployeeValidEarnCodePropertyDictionary.Add(ValidEarnCodeBase.Columns.EarnCodeId.ToString(), this.EarningCodeIdPropertyChanged);
            //Employee Withhold State 
            this.EmployeeWithholdStateCodePropertyDictionary.Add(WithholdState.Columns.Id.ToString(),this.TaxAuthorityIdPropertyChanged);
            this.EmployeeWithholdStateCodePropertyDictionary.Add(WithholdState.Columns.DefaultWH.ToString(),this.DefaultWHPropertyChanged);            
        }        

        protected override void ProcessCustomResponse(ApiEntityPropertyChangingArgs args)
        {
            if (args.Entity is DeductEmployee deductEmployee)
            {
                if (StringHelper.AreEqual(args.FieldName, "DedCodeId", false))
                {
                    args.ActualValue = deductEmployee.DeductionCode?.DeductionCode;
                }
            }
            if (args.Entity is DeductEmployer deductEmployer)
            {
                if (StringHelper.AreEqual(args.FieldName, "DedCodeId", false))
                {
                    args.ActualValue = deductEmployer.DeductionCode?.DeductionCode;
                }
            }
        }
        #endregion        

        protected virtual async Task<EntityList<Employee>> Load(string id)
        {
            if (this.Provider.Items.Count <= 0 || (id != null && !this.Provider.Items.Exists(i => id == i.Id)))
            {
                if (id == null)
                    await Provider.Load<Employee>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<Employee.Columns>();
                    builder.AppendEquals(Employee.Columns.EmployeeId, id);
                    var list = new EmployeeProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));
                    
                    this.Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Employee> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => x.EmployeeId == id);
        }

        protected virtual async Task<List<Employee>> ProcessEditRequest(bool isCreate, dynamic body, string id)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Employee ID is provided along with more than one record.");

            var entityList = new List<Employee>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }

            await this.ValidateEntityListAsync(entityList);
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<Employee> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EmployeeId) || string.IsNullOrEmpty(bodyItem.EmployeeId))
                bodyItem.Id = code;
            else
                code = bodyItem.EmployeeId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Employee(this.CompId);
                entity.EmployeeId = code;
                entity.SetDefaults();
            }
            else if (entity == null)
                throw new InvalidValueException($"Employee ID '{code}' could not be found.");
            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity, ProcessChildObject);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;
            
            return entity;
        }        

        protected virtual async Task MarkToDelete(string id)
        {
            var employee = await this.Find(id);

            if (employee == null)
                throw new NothingToProcessException($"Employee ID '{id}' could not be found.");
            else
            {
                this.Provider.Items.Remove(employee);
                this.Provider.Update(this.CompId);
            }
        }

        protected virtual object ProcessChildObject(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "Detail")
            {
                if (((Employee)args.ParentObject).IsNew)
                    ((Employee)args.ParentObject).CreateDetail();

                EmployeeDetail detail = ((Employee)args.ParentObject).Detail;
                detail.PropertyChanged += EmployeeDetail_PropertyChanged;

                return detail;
            }
            if (args.ParentObject is EmployeeDetail empDetail)
            {
                if (StringHelper.AreEqual(args.PropertyName, "ValidEarningCodes"))
                    if (empDetail.IsNew)
                        return this.CreateValidEarningCode((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdateValidEarningCode((EmployeeDetail)args.ParentObject, args.ItemModel);

                if (StringHelper.AreEqual(args.PropertyName, "FederalWithholdingCodes"))
                    if (empDetail.IsNew)
                        return CreateFederalWithholdingCode((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return UpdateFederalWithholdingCode((EmployeeDetail)args.ParentObject, args.ItemModel);

                if (StringHelper.AreEqual(args.PropertyName, "StateWithholdingCodes"))
                    if (empDetail.IsNew)
                        return CreateStateWithholdingCode((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return UpdateStateWithholdingCode((EmployeeDetail)args.ParentObject, args.ItemModel);

                if (StringHelper.AreEqual(args.PropertyName, "Bonuses"))
                {
                    if (empDetail.IsNew)
                        return this.CreateBonus((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdateBonus((EmployeeDetail)args.ParentObject, args.ItemModel);
                }
                if (StringHelper.AreEqual(args.PropertyName, "EmployeeDeductions"))
                {
                    if (empDetail.IsNew)
                        return this.CreateDeduction((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdateDeduction((EmployeeDetail)args.ParentObject, args.ItemModel);
                }
                if (StringHelper.AreEqual(args.PropertyName, "EmployerDeductions"))
                {
                    if (empDetail.IsNew)
                        return this.CreateEmployerDeduction((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdateEmployerDeduction((EmployeeDetail)args.ParentObject, args.ItemModel);
                }
                if (StringHelper.AreEqual(args.PropertyName, "PaymentDistributions"))
                {
                    if (empDetail.IsNew)
                        return this.CreatePayDistribution((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdatePayDistribution((EmployeeDetail)args.ParentObject, args.ItemModel);
                }
                if (StringHelper.AreEqual(args.PropertyName, "EducationDetails"))
                {
                    if (empDetail.IsNew)
                        return this.CreateEducation((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdateEducation((EmployeeDetail)args.ParentObject, args.ItemModel);
                }
                if (StringHelper.AreEqual(args.PropertyName, "PayChanges"))
                {
                    if (empDetail.IsNew)
                        return this.CreatePayChange((EmployeeDetail)args.ParentObject, args.ItemModel);
                    else
                        return this.UpdatePayChange((EmployeeDetail)args.ParentObject, args.ItemModel);
                }
            }
            return null;
        }

        #region Body Item Update Methods
        protected virtual void CodePropertyChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == "DedCodeId" && args.Entity is DeductEmployee entity)
            {
                  entity.DeductionCodeId = EntityProvider.GetEntityList<DeductCode, DeductCodeProvider>(this.CompId, null, null).Find(x => x.DeductionCode == args.ActualValue.ToString()).Id;
            }
            if (args.FieldName == "DedCodeId" && args.Entity is DeductEmployer entityEmplr)
            {
                entityEmplr.DeductionCodeId = EntityProvider.GetEntityList<DeductCode, DeductCodeProvider>(this.CompId, null, null).Find(x => x.DeductionCode == args.ActualValue.ToString()).Id;
            }
        }
        #endregion

        #region Employee Detail Update Methods
        protected virtual void TaxGroupIdChanged(EmployeeDetail entity)
        {
            if (entity == null || entity.TaxGroup == null || !entity.IsNew)
            {
                return;
            }
            foreach (TaxGroupDetail current in entity.TaxGroup.TaxGroupDetailList)
            {
                if (entity.LocalWithholdingCodes?.Find(WithholdBase.Columns.TaxAuthorityId, current.TaxAuthorityId) == null)
                {
                    WithholdLocal withholdLocal = new WithholdLocal
                    {
                        TaxAuthorityId = current.TaxAuthorityId
                    };
                    entity.LocalWithholdingCodes.Add(withholdLocal);
                }
            }
        }

        protected virtual void HourlyRateChanged(EmployeeDetail entity)
        {
            if (!entity.IsNew && entity.Type == EmployeeType.Hourly)
                this.AddHourlyRateChangeToPayChangeList(entity);
        }

        protected virtual void SalaryPropertyChanged(EmployeeDetail entity)
        {
            if (!entity.IsNew && entity.Type == EmployeeType.Salaried)
                this.AddSalaryRateChangeToPayChangeList(entity);
        }

        protected virtual void AddSalaryRateChangeToPayChangeList(EmployeeDetail detail)
        {
            EntityList<PayChange> entityList = new EntityList<PayChange>();
            var newPayChange = this.CreateNewPayChange(detail);
            newPayChange.NewRate = detail.Salary;
            newPayChange.Date = new DateTime?(ApplicationContext.SessionDate.Date);
            entityList.Add(newPayChange);
            detail.PayChanges.AddRange(entityList);
        }

        protected virtual void AddHourlyRateChangeToPayChangeList(EmployeeDetail detail)
        {
            EntityList<PayChange> entityList = new EntityList<PayChange>();
            var newPayChange = this.CreateNewPayChange(detail);
            newPayChange.NewRate = detail.HourlyRate;
            newPayChange.Date = new DateTime?(ApplicationContext.SessionDate.Date);
            entityList.Add(newPayChange);
            detail.PayChanges.AddRange(entityList);
        }

        protected virtual PayChange CreateNewPayChange(EmployeeDetail detail)
        {
            var payChange = new PayChange
            {
                Parent = detail
            };
            payChange.SetDefaults();
            return payChange;
        }

        protected virtual void ResetDefaultStateWH(WithholdState entity)
        {
            EntityList<WithholdState> withholdStateList = null;
            withholdStateList = entity.Parent?.StateWithholdingCodes?.FindAll(x => x.DefaultWH == true);

            if (withholdStateList?.Count > 0)
            {
                foreach (WithholdState item in withholdStateList)
                {
                    if (item.TaxAuthorityId != entity.TaxAuthorityId) item.DefaultWH = false;
                }
            }
        }

        protected virtual void ResetDefaultEarnCode(ValidEarnCode entity)
        {
            EntityList<ValidEarnCode> earnCodeList = null;
            earnCodeList = entity.Parent?.ValidEarningCodes?.FindAll(x => x.DefaultEarningCodeYn == true);

            if (earnCodeList?.Count > 0)
            {
                foreach (ValidEarnCode item in earnCodeList)
                {
                    if (!StringHelper.AreEqual(item.EarnCodeId, entity.EarnCodeId, false)) item.DefaultEarningCodeYn = false;
                }
            }
        }
        #endregion         
        
        #region Earning Codes Update Methods
        protected virtual ValidEarnCode UpdateValidEarningCode(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.EarnCodeId))
                throw new InvalidValueException("Earning Code ID is required.");

            this.FilterEntityListAsync(parent.ValidEarningCodes, ApiPaEmployeeEarningCodeController.FunctionID);

            ValidEarnCode earnCode = parent.ValidEarningCodes.Find(ValidEarnCode.Columns.EarnCodeId, bodyItem.EarnCodeId);
            if (earnCode == null)
                throw new InvalidValueException($"Earning Code ID {bodyItem.EarnCodeId} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            earnCode.PropertyChanged += EarnCode_PropertyChanged;
            Request.RegisterForDispose(earnCode);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return earnCode;
        }

        protected virtual ValidEarnCode CreateValidEarningCode(EmployeeDetail detail, dynamic bodyItem)
        {
            ValidEarnCode earnCode = detail.ValidEarningCodes?.Find(x => StringHelper.AreEqual(x.EarnCodeId, bodyItem.EarnCodeId, false));
            if (earnCode != null)
                throw new InvalidValueException($"Earning Code ID '{bodyItem.EarnCodeId}' already exists." );
            else
                earnCode = detail.ValidEarningCodes.AddNew();

            earnCode.PropertyChanged += EarnCode_PropertyChanged;
            earnCode.SetDefaults();
            earnCode.DefaultEarningCodeYn = (bool?)bodyItem.DefaultEarningCodeYn ?? false;
            return earnCode;
        }

        protected virtual void EarningCodeIdPropertyChanged(ValidEarnCode entity)
        {
            if (entity.IsNew && entity.DefaultEarningCodeYn == true)
                this.ResetDefaultEarnCode(entity);
        }
        #endregion

        #region Withholding Update Methods
        protected virtual WithholdFederal UpdateFederalWithholdingCode(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.id))
                throw new InvalidValueException("Federal WithHolding ID is required.");

            this.FilterEntityListAsync(parent.FederalWithholdingCodes, ApiPaEmployeeFederalWithholdingController.FunctionID);

            WithholdFederal federalWH = parent.FederalWithholdingCodes.Find(WithholdFederal.Columns.Id, (int)bodyItem.id);
            if (federalWH == null)
                throw new InvalidValueException($"Federal WithHolding ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;            
            Request.RegisterForDispose(federalWH);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return federalWH;
        }

        protected virtual WithholdFederal CreateFederalWithholdingCode(EmployeeDetail detail, dynamic bodyItem)
        {
            int id = ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) ? 0 : bodyItem.Id;
            WithholdFederal federalWH = detail.FederalWithholdingCodes?.Find(x => x.Id == id);
            if (federalWH != null)
                throw new InvalidValueException($"Federal WithHolding ID '{id}' already exists.");
            else
                federalWH = detail.FederalWithholdingCodes.AddNew();

            federalWH.TaxAuthorityId = this.FederalTaxAuthority.Id;
            federalWH.SetDefaults();
            return federalWH;
        }

        protected virtual WithholdState UpdateStateWithholdingCode(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.id))
                throw new InvalidValueException("Federal WithHolding ID is required.");

            this.FilterEntityListAsync(parent.FederalWithholdingCodes, ApiPaEmployeeStateWithholdingController.FunctionID);

            WithholdState stateWH = parent.StateWithholdingCodes.Find(WithholdState.Columns.Id, (int)bodyItem.id);
            if (stateWH == null)
                throw new InvalidValueException($"State WithHolding ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(stateWH);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return stateWH;
        }

        protected virtual WithholdState CreateStateWithholdingCode(EmployeeDetail detail, dynamic bodyItem)
        {
            int id = ApiUserSkipped.IsApiUserSkipped(bodyItem.Id) ? 0 : bodyItem.Id;
            WithholdState stateWH = detail.StateWithholdingCodes?.Find(x => x.Id == id && x.TaxAuthorityId == bodyItem.TaxAuthorityId);
            if (stateWH != null)
                throw new InvalidValueException($"State WithHolding ID'{id}' with Tax Authority ID '{bodyItem.TaxAuthorityId}' already exists.");
            else
                stateWH = detail.StateWithholdingCodes.AddNew();

            stateWH.PropertyChanged += StateWH_PropertyChanged;
            stateWH.SetDefaults();
            return stateWH;
        }

        protected virtual void DefaultWHPropertyChanged(WithholdState entity)
        {
            if (entity.IsNew && entity.DefaultWH == true)
                this.ResetDefaultStateWH(entity);
        }

        protected virtual void TaxAuthorityIdPropertyChanged(WithholdState entity)
        {
            if (entity.RequiresOverrideFactors && entity.OverrideFactors.Count == 0)
            {
                OverrideFactors oFactors = entity.OverrideFactors.AddNew();
                oFactors.SetDefaults();
                oFactors.TaxAuthorityDtlId = entity.TaxAuthorityId;
                oFactors.WithholdId = entity.Id;
            }
        }
        #endregion 

        #region Bonus Update Methods
        protected virtual Bonus UpdateBonus(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.id))
                throw new InvalidValueException("Bonus ID is required.");

            this.FilterEntityListAsync(parent.Bonuses, ApiPaEmployeeBonusController.FunctionID);

            Bonus bonus = parent.Bonuses.Find(Bonus.Columns.Id, (int)bodyItem.id);
            if (bonus == null)
                throw new InvalidValueException($"Bonus ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(bonus);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return bonus;
        }

        protected virtual Bonus CreateBonus(EmployeeDetail parent, dynamic bodyItem)
        {
            Bonus bonus = parent.Bonuses.AddNew();
            bonus.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(bonus);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return bonus;
        }
        #endregion 

        #region Deductions Update Methods
        protected virtual DeductEmployee UpdateDeduction(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.DedCodeId))
                throw new InvalidValueException("Deduction Code ID is required.");

            this.FilterEntityList(parent.EmployeeDeductions, ApiPaEmployeeDeductionController.FunctionID);

            DeductEmployee empDeduc = parent.EmployeeDeductions.Find(DeductEmployee.Columns.Id, (int)bodyItem.id);
            if (empDeduc == null)
                throw new InvalidValueException($"Deduction Code ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            empDeduc.PropertyChanged += EmployeeDeduction_PropertyChanged;
            Request.RegisterForDispose(empDeduc);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return empDeduc;
        }

        protected virtual DeductEmployee CreateDeduction(EmployeeDetail parent, dynamic bodyItem)
        {
            DeductEmployee empDeduc = parent.EmployeeDeductions.AddNew();
            empDeduc.SetDefaults();
            empDeduc.SeqNum = "1";

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            empDeduc.PropertyChanged += EmployeeDeduction_PropertyChanged;
            Request.RegisterForDispose(empDeduc);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return empDeduc;
        }

        protected virtual void DeductionCodeIdPropertyChanded(DeductEmployee entity)
        {
            if (entity.DeductionCode != null)
                entity.CalcOnGross = entity.DeductionCode.CalcOnGross;
        }
        #endregion 

        #region Employer Deductions - Employer Cost Update Methods
        protected virtual DeductEmployer UpdateEmployerDeduction(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.DedCodeId))
                throw new InvalidValueException("Cost Code ID is required.");

            this.FilterEntityList(parent.EmployerDeductions, ApiPaEmployeeDeductionEmployerController.FunctionID);

            DeductEmployer employerDeduc = parent.EmployerDeductions.Find(DeductEmployer.Columns.Id, (int)bodyItem.id);
            if (employerDeduc == null)
                throw new InvalidValueException($"Cost Code ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            employerDeduc.PropertyChanged += EmployerDeduction_PropertyChanged;
            Request.RegisterForDispose(employerDeduc);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return employerDeduc;
        }

        protected virtual DeductEmployer CreateEmployerDeduction(EmployeeDetail parent, dynamic bodyItem)
        {
            DeductEmployer employerDeduc = parent.EmployerDeductions.AddNew();
            employerDeduc.SetDefaults();
            employerDeduc.SeqNum = "1";

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            employerDeduc.PropertyChanged += EmployerDeduction_PropertyChanged;
            Request.RegisterForDispose(employerDeduc);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return employerDeduc;
        }

        protected virtual void DeductionCodeIdPropertyChanded(DeductEmployer entity)
        {
            if (entity.DeductionCode != null)
                entity.CalcOnGross = entity.DeductionCode.CalcOnGross;
        }
        #endregion 

        #region Pay Distribution - Direct Deposit Update Methods
        protected virtual PayDistribution UpdatePayDistribution(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Pay Distribution ID is required.");

            this.FilterEntityList(parent.PaymentDistributions, ApiPaEmployeePayDistributionController.FunctionID);

            PayDistribution payDistribution = parent.PaymentDistributions.Find(PayDistribution.Columns.Id, (int)bodyItem.id);
            if (payDistribution == null)
                throw new InvalidValueException($"Pay Distribution ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            payDistribution.PropertyChanged += EmployeePayDistribution_PropertyChanged;
            Request.RegisterForDispose(payDistribution);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return payDistribution;
        }

        protected virtual PayDistribution CreatePayDistribution(EmployeeDetail parent, dynamic bodyItem)
        {
            PayDistribution payDist = parent.PaymentDistributions.AddNew();
            payDist.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            payDist.PropertyChanged += EmployeePayDistribution_PropertyChanged;
            Request.RegisterForDispose(payDist);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return payDist;
        }

        protected virtual void AmountPercentPropertyChanged(PayDistribution entity)
        {
            entity.Validate(EmployeeDetailBase.Columns.PayDistribution.ToString());
        }

        protected virtual void AccountNumberPropertyChanged(PayDistribution entity)
        {
            entity.PrenoteIn = false;
        }
        #endregion 

        #region Education Update Methods
        protected virtual Education UpdateEducation(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Education ID is required.");

            this.FilterEntityList(parent.PaymentDistributions, ApiPaEmployeePayDistributionController.FunctionID);

            Education education = parent.EducationDetails.Find(Education.Columns.Id, (int)bodyItem.id);
            if (education == null)
                throw new InvalidValueException($"Education ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(education);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return education;
        }

        protected virtual Education CreateEducation(EmployeeDetail parent, dynamic bodyItem)
        {
            Education education = parent.EducationDetails.AddNew();
            education.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(education);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return education;
        }
        #endregion 

        #region Pay Change Update Methods
        protected virtual PayChange UpdatePayChange(EmployeeDetail parent, dynamic bodyItem)
        {
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.Id))
                throw new InvalidValueException("Pay Change ID is required.");

            this.FilterEntityList(parent.PayChanges, ApiPaEmployeeRateChangeController.FunctionID);

            PayChange payChange = parent.PayChanges.Find(PayChange.Columns.Id, (int)bodyItem.id);
            if (payChange == null)
                throw new InvalidValueException($"Pay Change ID {bodyItem.id} could not be found on Employee {parent.EmployeeId}");

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(payChange);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return payChange;
        }

        protected virtual PayChange CreatePayChange(EmployeeDetail parent, dynamic bodyItem)
        {
            PayChange payChange = parent.PayChanges.AddNew();
            payChange.SetDefaults();

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            Request.RegisterForDispose(payChange);
            Request.RegisterForDispose((ApiEntityModel)bodyItem);

            return payChange;
        }
        #endregion
        #endregion Helper Methods

        #region Event Handlers
        private void BodyItem_EntityPropertyChanging(object sender, ApiEntityPropertyChangingArgs e)
        {
            Action<dynamic, ApiEntityPropertyChangingArgs> action = null;
            if (this.EntityPropertyDictionary.TryGetValue(e.FieldName, out action))
                action.Invoke(sender as dynamic, e);
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<Employee> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Employee);
        }

        private void EmployeeDeduction_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<DeductEmployee> action = null;
            if (this.EmployeeDeductionsPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as DeductEmployee);
        }

        private void EmployerDeduction_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<DeductEmployer> action = null;
            if (this.EmployerDeductionsPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as DeductEmployer);
        }

        private void EmployeePayDistribution_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<PayDistribution> action = null;
            if (this.EmployeePayDistributionPropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as PayDistribution);
        }
                
        private void EmployeeDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.EmployeeDetailPropertyDictionary.TryGetValue(e.PropertyName, out Action<EmployeeDetail> action))
                action.Invoke(sender as EmployeeDetail);
        }
        
        private void EarnCode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.EmployeeValidEarnCodePropertyDictionary.TryGetValue(e.PropertyName, out Action<ValidEarnCode> action))
                action.Invoke(sender as ValidEarnCode); 
        }
        
        private void StateWH_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.EmployeeWithholdStateCodePropertyDictionary.TryGetValue(e.PropertyName, out Action<WithholdState> action))
                action.Invoke(sender as WithholdState);
        }        
        #endregion Event Handlers

        #region Properties
        protected EmployeeProvider Provider { get; } = new EmployeeProvider();

        protected Employee CurrentEmployee { get; set; }

        protected SortedDictionary<string, Action<Employee>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Employee>>();

        protected SortedDictionary<string, Action<EmployeeDetail>> EmployeeDetailPropertyDictionary { get; } = new SortedDictionary<string, Action<EmployeeDetail>>();

        protected SortedDictionary<string, Action<DeductEmployee>> EmployeeDeductionsPropertyDictionary { get; } = new SortedDictionary<string, Action<DeductEmployee>>();

        protected SortedDictionary<string, Action<DeductEmployer>> EmployerDeductionsPropertyDictionary { get; } = new SortedDictionary<string, Action<DeductEmployer>>();

        protected SortedDictionary<string, Action<PayDistribution>> EmployeePayDistributionPropertyDictionary { get; } = new SortedDictionary<string, Action<PayDistribution>>();

        protected SortedDictionary<string, Action<Education>> EmployeeEducationPropertyDictionary { get; } = new SortedDictionary<string, Action<Education>>();

        protected SortedDictionary<string, Action<PayChange>> EmployeePayChangePropertyDictionary { get; } = new SortedDictionary<string, Action<PayChange>>();

        protected SortedDictionary<string, Action<ValidEarnCode>> EmployeeValidEarnCodePropertyDictionary { get; } = new SortedDictionary<string, Action<ValidEarnCode>>();

        protected SortedDictionary<string, Action<WithholdState>> EmployeeWithholdStateCodePropertyDictionary { get; } = new SortedDictionary<string, Action<WithholdState>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        protected virtual TaxAuthorityHeaderFederal FederalTaxAuthority
        {
            get
            {
                EntityList<TaxAuthorityHeaderFederal> list = ListFactory.CreateList<TaxAuthorityHeaderFederal, TaxAuthorityHeaderFederalProvider>(this.CompId);
                if (list.Count > 0)
                    return list[0];
                return null;
            }
        }        
        #endregion Properties 

        #region Fields
        public const string FunctionID = "0391BF9F-E1DC-488F-9805-EF16C3B94A5A";        
        #endregion Fields
    }
}
