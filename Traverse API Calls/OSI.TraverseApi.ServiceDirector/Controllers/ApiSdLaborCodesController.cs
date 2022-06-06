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
    public class ApiSdLaborCodesController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "laborcode/{laborcodeid?}", typeof(LaborCode))]
        public async Task<IHttpActionResult> Get(string laborCodeId = null)
        {
            return Ok(await this.Load(laborCodeId));
        }

        [ApiRoute(FunctionID, 2f, "laborcode/{laborcodeid?}", typeof(LaborCode))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string laborCodeId = null)
        {
            List<LaborCode> laborCodeList = new List<LaborCode>();
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(laborCodeId))
                throw new InvalidValueException("Call is ambiguous. Labor Code is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var laborCode = await this.UpdateLaborCode(item, laborCodeId);
                if (!laborCodeList.Contains(laborCode))
                    laborCodeList.Add(laborCode);
            }
            await Task.Run(() =>
            {
                this.ValidateEntityList(laborCodeList);
                this.Provider?.Update(this.CompId);
            });

            return Ok(laborCodeList);
        }

        [ApiRoute(FunctionID, 2f, "laborcode/{laborcodeid?}", typeof(LaborCode))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string laborCodeId = null)
        {
            List<LaborCode> laborCodeList = new List<LaborCode>();
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(laborCodeId))
                throw new InvalidValueException("Call is ambiguous. Labor Code is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var laborCode = await this.CreateLaborCode(item, laborCodeId);
                this.Provider.Items.Add(laborCode);

                if (!laborCodeList.Contains(laborCode))
                    laborCodeList.Add(laborCode);
            }

            await Task.Run(() =>
            {
                this.ValidateEntityList(laborCodeList);
                this.Provider.Update(this.CompId);
            });

            return Ok(laborCodeList);
        }

        [ApiRoute(FunctionID, 2f, "laborcode/{laborcodeid}", typeof(LaborCode))]
        public async Task<IHttpActionResult> Delete(string laborCodeId = null)
        {
            await this.MarkToDelete(laborCodeId);

            await Task.Run(() =>
            {
                this.Provider.Update(CompId);
            });

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<LaborCode>> Load(string laborCodeId)
        {
            return await Task.Run(() =>
            {
                SqlFilterBuilder<LaborCodeBase.Columns> builder = new SqlFilterBuilder<LaborCodeBase.Columns>();
                if (!string.IsNullOrEmpty(laborCodeId))
                    builder.AppendEquals(LaborCodeBase.Columns.LaborCode, laborCodeId);

                this.Provider.CompId = this.CompId;
                this.Provider.SetPage(PageNumber, PageSize);
                var list = this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                //This Filter line is required so that the user does not receive data not intended for them to see
                this.FilterEntityList(list);
                return list;
            });
        }

        private async Task<LaborCode> UpdateLaborCode(dynamic bodyItem, string laborCodeId)
        {
            LaborCode code = await this.Find(ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborCode) ? laborCodeId : bodyItem.LaborCode);
            if (code == null)
                throw new NothingToProcessException(string.Format("Labor Code '{0}' could not be found.", ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborCode) ? laborCodeId : bodyItem.LaborCode));

            code.PropertyChanged += LaborCode_PropertyChanged;
            ((ApiEntityModel)bodyItem).PopulateEntity(code);
            code.PropertyChanged -= LaborCode_PropertyChanged;

            return code;
        }

        private async Task<LaborCode> CreateLaborCode(dynamic bodyItem, string laborCodeId)
        {
            LaborCode code = await this.Find(ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborCode) ? laborCodeId : bodyItem.LaborCode);

            if (code != null)
                throw new InvalidValueException(string.Format("Labor Code '{0}' already exists.", ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborCode) ? laborCodeId : bodyItem.LaborCode));
            else
                code = new LaborCode(this.CompId);

            code.PropertyChanged += LaborCode_PropertyChanged;
            code.LaborCode = ApiUserSkipped.IsApiUserSkipped(bodyItem.LaborCode) ? laborCodeId : bodyItem.LaborCode;
            ((ApiEntityModel)bodyItem).PopulateEntity(code);
            code.PropertyChanged -= LaborCode_PropertyChanged;

            return code;
        }

        private async Task MarkToDelete(string laborCodeId)
        {
            LaborCode code = await this.Find(laborCodeId);

            if (code == null)
                throw new NothingToProcessException(string.Format("Labor Code '{0}' could not be found.", laborCodeId));
            else
                this.Provider.Items.Remove(code);
        }

        private async Task<LaborCode> Find(string laborCodeId)
        {
            var code = this.Provider?.Items?.Find(x => StringHelper.AreEqual(x.LaborCode, laborCodeId, false));

            if (code == null)
            {
                await Task.Run(() =>
                { 
                    code = EntityProvider.GetEntity<LaborCode, LaborCodeProvider>(new string[] { laborCodeId }, this.CompId, null);
                    if (code != null)
                    this.Provider.Items.Add(code);
            });
        }
            return code;
        }
        #endregion Helper Methods

        #region Event Handlers
        private void LaborCode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnLaborCode_PropertyChanged(sender as LaborCode, e);
        }
        public virtual void OnLaborCode_PropertyChanged(LaborCode entity, PropertyChangedEventArgs e)
        {
        }
        #endregion Event Handlers

        #region Properties
        private LaborCodeProvider Provider { get => this._provider != null ? _provider : (this._provider = new LaborCodeProvider()); }

        private const string FunctionID = "A29FE3E5-99B6-4688-A2B9-AFD98539AA14";
        #endregion Properties

        #region Fields
        private LaborCodeProvider _provider;
        #endregion Fields
    }
}
