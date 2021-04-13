#region Using Directives
using OSI.TraverseApi.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Contacts;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Contacts.Controllers
{
    public class ApiVendorController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "vendor/{id?}", typeof(Vendor))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "vendor/{id?}", typeof(Vendor))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "vendor/{id?}", typeof(Vendor))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "vendor/{id}", typeof(Vendor))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates()
        {
            EntityPropertyDictionary.Add(VendorBase.Columns.Ten99ForeignAddrYN.ToString(), Ten99StatusChanged);
            EntityPropertyDictionary.Add(VendorBase.Columns.SecondTINNotYN.ToString(), Ten99StatusChanged);
        }

        protected virtual async Task<EntityList<Vendor>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.VendorId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Vendor>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<Vendor.Columns>();
                    builder.AppendEquals(Vendor.Columns.VendorId, id);
                    var list = new VendorProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Vendor> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.VendorId, id, false));
        }

        protected virtual async Task<List<Vendor>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Vendor ID is provided along with more than one record.");

            var entityList = new List<Vendor>();
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

        protected virtual async Task<Vendor> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.VendorId) || string.IsNullOrWhiteSpace(bodyItem.VendorId))
                bodyItem.VendorId = code;
            else
                code = bodyItem.VendorId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Vendor(this.CompId);
                entity.SetDefaultValues();
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Vendor '{0}' could not be found.", code));

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
                throw new NothingToProcessException(string.Format("Vendor '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
        }

        protected virtual void Ten99StatusChanged(dynamic bodyItem, ApiEntityPropertyChangingArgs e)
        {
            if (!ApiUserSkipped.IsApiUserSkipped(e.ActualValue))
            {
                if (StringHelper.AreEqual(((Vendor)e.Entity).Ten99FormCode, "0", false))
                    e.Handled = true;
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
            Action<Vendor> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Vendor);
        }
        #endregion Event Handlers

        #region Properties
        protected VendorProvider Provider { get; } = new VendorProvider();

        protected SortedDictionary<string, Action<Vendor>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Vendor>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "1AB8E9D6-9968-4CFD-84EB-407ECB081810";
        #endregion Fields
    }
}
