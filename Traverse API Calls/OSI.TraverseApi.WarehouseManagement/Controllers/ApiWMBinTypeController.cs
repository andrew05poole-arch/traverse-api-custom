#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.WMS;
using TRAVERSE.Core;
using TraverseApi;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace OSI.TraverseApi.WarehouseManagement.Controllers
{
    public class ApiWMBinTypeController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "bintypes/{id?}", typeof(ExtLocationType))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "bintypes/{id?}", typeof(ExtLocationType))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "bintypes/{id?}", typeof(ExtLocationType))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "bintypes/{id}", typeof(ExtLocationType))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        #region Overrides
        protected override void AddPropertyDelegates() { }
        #endregion

        protected virtual async Task<EntityList<ExtLocationType>> Load(string id)
        {
            if (this.Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !this.Provider.Items.Exists(i => StringHelper.AreEqual(id, i.Description, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<ExtLocationType>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<ExtLocationType.Columns>();
                    builder.AppendEquals(ExtLocationType.Columns.Description, id);
                    var list = new ExtLocationTypeProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(this.Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<ExtLocationType> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.Description, id, false));
        }

        protected virtual async Task<List<ExtLocationType>> ProcessEditRequest(bool isCreate, dynamic bodyItem, string id = null)
        {
            object[] list;

            if (bodyItem is object[])
                list = bodyItem;
            else
                list = new object[1] { bodyItem };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Bin Type is provided along with more than one record.");

            var entityList = new List<ExtLocationType>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(isCreate, item, id);
                this.Provider.Items.Add(entity);

                if (!entityList.Contains(entity))
                    entityList.Add(entity);

                await ValidateEntityListAsync(entityList);                
            }
            this.Provider.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<ExtLocationType> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = ApiUserSkipped.IsApiUserSkipped(bodyItem.Description) || !string.IsNullOrWhiteSpace(id) ? (bodyItem.Description = id) : bodyItem.Description;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new ExtLocationType(this.CompId);
                entity.Id = this.Provider.GetNextId();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Bin Type '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var employee = await this.Find(id);

            if (employee == null)
                throw new NothingToProcessException(string.Format("Bin Type '{0}' could not be found.",id));
            else
            {
                this.Provider.Items.Remove(employee);
                this.Provider.Update(this.CompId);
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
            Action<ExtLocationType> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as ExtLocationType);
        }
        #endregion  Event Handlers

        #region Properties
        protected ExtLocationTypeProvider Provider { get; } = new ExtLocationTypeProvider();

        protected SortedDictionary<string, Action<ExtLocationType>> PropertyDictionary { get; } = new SortedDictionary<string, Action<ExtLocationType>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        public const string FunctionID = "e5d062aa-48f6-457a-a591-38b99238ced9";
        #endregion Properties
    }
}
