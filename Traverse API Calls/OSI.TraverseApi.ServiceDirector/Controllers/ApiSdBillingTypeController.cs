#region Using Directives
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.ServiceDirector;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.ServiceDirector.Controllers
{
    public class ApiSdBillingTypeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "billingtype/{billingtypeid?}", typeof(BillingType))]
        public async Task<IHttpActionResult> Get(string billingTypeId = null)
        {
            return Ok(await this.Load(billingTypeId));
        }

        [ApiRoute(FunctionID, 2f, "billingtype/{billingtypeid?}", typeof(BillingType))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string billingTypeId = null)
        {
            List<BillingType> billingTypeList = new List<BillingType>();
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(billingTypeId))
                throw new InvalidValueException("Call is ambiguous. Billing Type is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var billingType = await this.UpdateBillingType(item, billingTypeId);
                if (!billingTypeList.Contains(billingType))
                    billingTypeList.Add(billingType);
            }
            await Task.Run(() =>
            {
                this.ValidateEntityList(billingTypeList);
                this.Provider?.Update(this.CompId);
            });

            return Ok(billingTypeList);
        }

        [ApiRoute(FunctionID, 2f, "billingtype/{billingtypeid?}", typeof(BillingType))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string billingTypeId = null)
        {
            List<BillingType> billingTypeList = new List<BillingType>();
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(billingTypeId))
                throw new InvalidValueException("Call is ambiguous. Billing Type is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var billingType = await this.CreateBillingType(item, billingTypeId);
                this.Provider.Items.Add(billingType);

                if (!billingTypeList.Contains(billingType))
                    billingTypeList.Add(billingType);
            }

            await Task.Run(() =>
            {
                this.ValidateEntityList(billingTypeList);
                this.Provider.Update(this.CompId);
            });

            return Ok(billingTypeList);
        }

        [ApiRoute(FunctionID, 2f, "billingtype/{billingtypeid}", typeof(BillingType))]
        public async Task<IHttpActionResult> Delete(string billingTypeId = null)
        {
            await this.MarkToDelete(billingTypeId);

            await Task.Run(() =>
            {
                this.Provider.Update(CompId);
            });

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<BillingType>> Load(string billingTypeId)
        {
            return await Task.Run(() =>
            {
                SqlFilterBuilder<BillingTypeBase.Columns> builder = new SqlFilterBuilder<BillingTypeBase.Columns>();
                if (!string.IsNullOrEmpty(billingTypeId))
                    builder.AppendEquals(BillingTypeBase.Columns.BillingType, billingTypeId);

                this.Provider.CompId = this.CompId;
                this.Provider.SetPage(PageNumber, PageSize);
                var list = this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                //This Filter line is required so that the user does not receive data not intended for them to see
                this.FilterEntityList(list);
                return list;
            });
        }

        private async Task<BillingType> UpdateBillingType(dynamic bodyItem, string billingTypeId)
        {
            BillingType billingType = await this.Find(ApiUserSkipped.IsApiUserSkipped(bodyItem.BillingType) ? billingTypeId : bodyItem.BillingType);
            if (billingType == null)
                throw new NothingToProcessException(string.Format("Billing Type '{0}' could not be found.", ApiUserSkipped.IsApiUserSkipped(bodyItem.BillingType) ? billingTypeId : bodyItem.BillingType));

            billingType.PropertyChanged += BillingType_PropertyChanged;
            ((ApiEntityModel)bodyItem).PopulateEntity(billingType);
            billingType.PropertyChanged -= BillingType_PropertyChanged;

            return billingType;
        }

        private async Task<BillingType> CreateBillingType(dynamic bodyItem, string billingTypeId)
        {
            BillingType billingType = await this.Find(ApiUserSkipped.IsApiUserSkipped(bodyItem.BillingType) ? billingTypeId : bodyItem.BillingType);

            if (billingType != null)
                throw new InvalidValueException(string.Format("Billing Type '{0}' already exists.", ApiUserSkipped.IsApiUserSkipped(bodyItem.BillingType) ? billingTypeId : bodyItem.BillingType));
            else
                billingType = new BillingType(this.CompId);

            billingType.PropertyChanged += BillingType_PropertyChanged;
            billingType.SetDefaults();
            billingType.BillingType = ApiUserSkipped.IsApiUserSkipped(bodyItem.BillingType) ? billingTypeId : bodyItem.BillingType;
            ((ApiEntityModel)bodyItem).PopulateEntity(billingType);
            billingType.PropertyChanged -= BillingType_PropertyChanged;

            return billingType;
        }

        private async Task MarkToDelete(string billingTypeId)
        {
            BillingType billingType = await this.Find(billingTypeId);

            if (billingType == null)
                throw new NothingToProcessException(string.Format("Billing Type '{0}' could not be found.", billingTypeId));
            else
                this.Provider.Items.Remove(billingType);
        }

        private async Task<BillingType> Find(string billingTypeId)
        {
            var billingType = this.Provider?.Items?.Find(x => StringHelper.AreEqual(x.BillingType, billingTypeId, false));

            if (billingType == null)
            {
                await Task.Run(() =>
                {
                    billingType = EntityProvider.GetEntity<BillingType, BillingTypeProvider>(new string[] { billingTypeId }, this.CompId, null);
                    if (billingType != null)
                        this.Provider.Items.Add(billingType);
                });
            }
            return billingType;
        }
        #endregion Helper Methods

        #region Event Handlers
        private void BillingType_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnBillingType_PropertyChanged(sender as BillingType, e);
        }
        public virtual void OnBillingType_PropertyChanged(BillingType entity, PropertyChangedEventArgs e)
        {
        }
        #endregion Event Handlers

        #region Properties
        private BillingTypeProvider Provider { get => this._provider != null ? _provider : (this._provider = new BillingTypeProvider()); }

        private const string FunctionID = "C73815E4-F417-431D-8CF7-B4227FAAF25F";
        #endregion Properties

        #region Fields
        private BillingTypeProvider _provider;
        #endregion Fields
    }
}
