#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Inventory;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Inventory.Controllers
{
    public class ApiInItemPictureController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "picture/{id?}", typeof(Picture))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "picture/{id?}", typeof(Picture))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "picture/{id?}", typeof(Picture))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "picture/{id}", typeof(Picture))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            EntityPropertyDictionary.Add(PictureBase.Columns.PictItem.ToString(), PictureItemChanging);
        }

        protected virtual async Task<EntityList<Picture>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.PictId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Picture>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<Picture.Columns>();
                    builder.AppendEquals(Picture.Columns.PictId, id);
                    var list = new PictureProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Picture> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.PictId, id, false));
        }

        protected virtual async Task<List<Picture>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Picture ID is provided along with more than one record.");

            var entityList = new List<Picture>();
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

        protected virtual async Task<Picture> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = ApiUserSkipped.IsApiUserSkipped(bodyItem.PictId) ? id : bodyItem.PictId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Picture(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Picture record '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Picture record '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider.Update(this.CompId);
        }

        protected virtual void PictureItemChanging(dynamic sender, ApiEntityPropertyChangingArgs args)
        {
            if (args.FieldName == PictureBase.Columns.PictItem.ToString())
            {
                ((Picture)args.Entity).PictItem = (args.ActualValue == null) ? null : Convert.FromBase64String(args.ActualValue.ToString());
                args.Handled = true;
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
            Action<Picture> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Picture);
        }
        #endregion Event Handlers

        #region Properties
        protected PictureProvider Provider { get; } = new PictureProvider();

        protected SortedDictionary<string, Action<Picture>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Picture>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();

        private const string FunctionID = "70D1F05B-1E0B-4101-A535-2B2CEC2834EB";
        #endregion Properties
    }
}
