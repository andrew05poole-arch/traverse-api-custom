#region Using Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.GeneralLedger;
using TRAVERSE.Core;
using TRAVERSE.Web.API;
using TRAVERSE.Business.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.GeneralLedger.Controllers
{
    public class ApiGlChartOfAccountController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "account/{id?}", typeof(Account))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await this.Load(id));
        }

        [ApiRoute(FunctionID, 2f, "account/{id?}", typeof(Account))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(false, body, id));
        }

        [ApiRoute(FunctionID, 2f, "account/{id?}", typeof(Account))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string id = null)
        {
            return Ok(await ProcessEditRequest(true, body, id));
        }

        [ApiRoute(FunctionID, 2f, "account/{id}", typeof(Account))]
        public async Task Delete(string id)
        {
            await this.MarkToDelete(id);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<Account>> Load(string id)
        {
            if (Provider.Items.Count <= 0 || (!string.IsNullOrEmpty(id) && !Provider.Items.Exists(i => StringHelper.AreEqual(id, i.AcctId, false))))
            {
                if (string.IsNullOrEmpty(id))
                    await Provider.Load<Account>(this.CompId, PageNumber, PageSize);
                else
                {
                    var builder = new SqlFilterBuilder<Account.Columns>();
                    builder.AppendEquals(Account.Columns.AcctId, id);
                    var list = new AccountProvider().Load(this.CompId, new FilterCriteria(builder.ToString(), ""));
                    Provider.Items.AddRange(list);
                }

                await this.FilterEntityListAsync(Provider.Items);
            }

            return Provider.Items;
        }

        protected virtual async Task<Account> Find(string id)
        {
            var list = await Load(id);
            return list.Find(x => StringHelper.AreEqual(x.AcctId, id, false));
        }

        protected virtual async Task<List<Account>> ProcessEditRequest(bool isCreate, dynamic body, string id = null)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. Account ID is provided along with more than one record.");

            var entityList = new List<Account>();
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

        protected virtual async Task<Account> ProcessBodyItem(bool isCreate, dynamic bodyItem, string id)
        {
            string code = id;
            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.AcctId) || string.IsNullOrWhiteSpace(bodyItem.AcctId))
                bodyItem.AcctId = code;
            else
                code = bodyItem.AcctId;

            var entity = await this.Find(code);

            if (isCreate)
            {
                if (entity != null)
                    return entity;

                entity = new Account(this.CompId);
            }
            else if (entity == null)
                throw new InvalidValueException(string.Format("Account '{0}' could not be found.", code));

            ((ApiEntityModel)bodyItem).EntityPropertyChanging += BodyItem_EntityPropertyChanging;
            entity.PropertyChanged += Entity_PropertyChanged;
            await ((ApiEntityModel)bodyItem).PopulateEntityAsync(entity);
            foreach (Segment segment in entity.SegmentList)
            {
                ValidateSegments(segment, entity);
            }
            entity.PropertyChanged -= Entity_PropertyChanged;
            ((ApiEntityModel)bodyItem).EntityPropertyChanging -= BodyItem_EntityPropertyChanging;

            return entity;
        }

        protected virtual void ValidateSegments (Segment segment, Account entity)
        {
            int segmentNumber = segment.Number;
            if (!string.IsNullOrEmpty(segment.Id) && SegmentProvider.GetSegment(segmentNumber, segment.Id.ToString(), this.CompId) == null)
            {
                entity.SegmentList[segmentNumber - 1].Id = segment.Id.ToString();
                //create the missing segment
                AccountQuickEntry.CreateMissingSegments(entity.SegmentList[segmentNumber - 1], this.CompId, null);
                entity.SegmentList[segmentNumber - 1].Validate();
            }
        }

        protected virtual async Task MarkToDelete(string id)
        {
            var entity = await this.Find(id);

            if (entity == null)
                throw new NothingToProcessException(string.Format("Account '{0}' could not be found.", id));

            this.Provider.Items.Remove(entity);
            this.Provider?.Update(this.CompId);
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
            Action<Account> action = null;
            if (PropertyDictionary.TryGetValue(e.PropertyName, out action))
                action.Invoke(sender as Account);
        }
        #endregion Event Handlers

        #region Properties
        protected AccountProvider Provider { get; } = new AccountProvider();

        protected SortedDictionary<string, Action<Account>> PropertyDictionary { get; } = new SortedDictionary<string, Action<Account>>();

        protected SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>> EntityPropertyDictionary { get; } = new SortedDictionary<string, Action<dynamic, ApiEntityPropertyChangingArgs>>();
        #endregion Properties

        #region Fields
        private const string FunctionID = "18351085-85FB-4F8E-AC92-A245A6532C1D";
        #endregion Fields
    }
}
