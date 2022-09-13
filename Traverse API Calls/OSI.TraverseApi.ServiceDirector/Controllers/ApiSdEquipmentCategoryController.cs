#region Using Directives
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.ServiceDirector;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.ServiceDirector.Controllers
{
    public class ApiSdEquipmentCategoryController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "equipmentcategory/{categoryid?}", typeof(EquipmentCategory))]
        public async Task<IHttpActionResult> Get(string categoryId = null)
        {
            return Ok(await this.Load(categoryId));
        }

        [ApiRoute(FunctionID, 2f, "equipmentcategory/{categoryid?}", typeof(EquipmentCategory))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string categoryId = null)
        {
            List<EquipmentCategory> equipmentCategoryList = new List<EquipmentCategory>();
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(categoryId))
                throw new InvalidValueException("Call is ambiguous. Category ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var equipmentCategory = await this.UpdateEquipmentCategory(item, categoryId);
                if (!equipmentCategoryList.Contains(equipmentCategory))
                    equipmentCategoryList.Add(equipmentCategory);
            }
            await Task.Run(() =>
            {
                this.ValidateEntityList(equipmentCategoryList);
                this.Provider?.Update(this.CompId);
            });

            return Ok(equipmentCategoryList);
        }

        [ApiRoute(FunctionID, 2f, "equipmentcategory/{categoryid?}", typeof(EquipmentCategory))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string categoryId = null)
        {
            List<EquipmentCategory> equipmentCategoryList = new List<EquipmentCategory>();
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(categoryId))
                throw new InvalidValueException("Call is ambiguous. Category ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var equipmentCategory = await this.CreateEquipmentCategory(item, categoryId);
                this.Provider.Items.Add(equipmentCategory);

                if (!equipmentCategoryList.Contains(equipmentCategory))
                    equipmentCategoryList.Add(equipmentCategory);
            }
            await Task.Run(() =>
            {
                this.ValidateEntityList(equipmentCategoryList);
                this.Provider?.Update(CompId);
            });
            return Ok(equipmentCategoryList);
        }

        [ApiRoute(FunctionID, 2f, "equipmentcategory/{categoryid}", typeof(EquipmentCategory))]
        public async Task<IHttpActionResult> Delete(string categoryId = null)
        {
            await this.MarkToDelete(categoryId);

            await Task.Run(() =>
            {
                this.Provider?.Update(CompId);
            });

            return Ok(null);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<EquipmentCategory>> Load(string categoryId)
        {
            return await Task.Run(() =>
            {
                SqlFilterBuilder<EquipmentCategoryBase.Columns> builder = new SqlFilterBuilder<EquipmentCategoryBase.Columns>();
                if (!string.IsNullOrEmpty(categoryId))
                    builder.AppendEquals(EquipmentCategoryBase.Columns.CategoryId, categoryId);

                this.Provider.CompId = this.CompId;
                this.Provider.SetPage(PageNumber, PageSize);
                var list = this.Provider.Load(CompId, new FilterCriteria(builder.ToString(), string.Empty));

                FilterEntityList(list);
                return list;
            });
        }

        private async Task<EquipmentCategory> UpdateEquipmentCategory(dynamic bodyItem, string categoryId)
        {
            EquipmentCategory equipmentCategory = await this.Find(ApiUserSkipped.IsApiUserSkipped(bodyItem.CategoryId) ? categoryId : bodyItem.CategoryId);
            if (equipmentCategory == null)
                throw new NothingToProcessException(string.Format("Category ID '{0}' could not be found.", ApiUserSkipped.IsApiUserSkipped(bodyItem.CategoryId) ? categoryId : bodyItem.CategoryId));

            equipmentCategory.PropertyChanged += EquipmentCategory_PropertyChanged;
            ((ApiEntityModel)bodyItem).PopulateEntity(equipmentCategory);
            equipmentCategory.PropertyChanged -= EquipmentCategory_PropertyChanged;

            return equipmentCategory;
        }

        private async Task<EquipmentCategory> CreateEquipmentCategory(dynamic bodyItem, string categoryId)
        {
            EquipmentCategory equipmentCategory = await this.Find(ApiUserSkipped.IsApiUserSkipped(bodyItem.CategoryId) ? categoryId : bodyItem.CategoryId);

            if (equipmentCategory != null)
                throw new InvalidValueException(string.Format("Category ID '{0}' already exists.", ApiUserSkipped.IsApiUserSkipped(bodyItem.CategoryId) ? categoryId : bodyItem.CategoryId));
            else
                equipmentCategory = new EquipmentCategory(this.CompId);

            equipmentCategory.PropertyChanged += EquipmentCategory_PropertyChanged;
            equipmentCategory.CategoryId = ApiUserSkipped.IsApiUserSkipped(bodyItem.CategoryId) ? categoryId : bodyItem.CategoryId;
            ((ApiEntityModel)bodyItem).PopulateEntity(equipmentCategory);
            equipmentCategory.PropertyChanged -= EquipmentCategory_PropertyChanged;

            return equipmentCategory;
        }

        private async Task MarkToDelete(string categoryId)
        {
            EquipmentCategory equipmentCategory = await this.Find(categoryId);

            if (equipmentCategory == null)
                throw new NothingToProcessException(string.Format("Category ID '{0}' could not be found.", categoryId));
            else
                this.Provider.Items.Remove(equipmentCategory);
        }

        private async Task<EquipmentCategory> Find(string categoryId)
        {
            var equipmentCategory = this.Provider?.Items?.Find(x => StringHelper.AreEqual(x.CategoryId, categoryId, false));

            if (equipmentCategory == null)
            {
                await Task.Run(() =>
                {
                    equipmentCategory = EntityProvider.GetEntity<EquipmentCategory, EquipmentCategoryProvider>(new string[] { categoryId }, this.CompId, null);
                    if (equipmentCategory != null)
                        this.Provider.Items.Add(equipmentCategory);
                });
            }
            return equipmentCategory;
        }
        #endregion Helper Methods

        #region Event Handlers
        private void EquipmentCategory_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnEquipmentCategory_PropertyChanged(sender as EquipmentCategory, e);
        }
        public virtual void OnEquipmentCategory_PropertyChanged(EquipmentCategory equipmentCategory, PropertyChangedEventArgs e)
        {

        }
        #endregion Event Handlers

        #region Properties
        private EquipmentCategoryProvider Provider { get => this._provider != null ? _provider : (this._provider = new EquipmentCategoryProvider()); }

        private const string FunctionID = "70DD48BD-6EE6-4C45-9BB0-DA93FBF24168";
        #endregion Properties

        #region Fields
        private EquipmentCategoryProvider _provider;
        #endregion Fields
    }
}
