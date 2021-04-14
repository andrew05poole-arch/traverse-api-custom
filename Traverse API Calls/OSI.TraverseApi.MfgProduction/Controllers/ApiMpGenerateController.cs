#region Using Directives
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.Manufacturing;
using TRAVERSE.Core;
using TraverseApi;
#endregion Using Directives

namespace OSI.TraverseApi.Manufacturing.Controllers
{
    public class ApiMpGenerateController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "releaseorders", typeof(OrderReleases))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string orderNo = null, int id = 0)
        {
            return Ok(await ProcessEditRequest(body, orderNo, id));
        }

        [ApiRoute(FunctionID, 2f, "order/{orderno}/release/{id}/generate", typeof(OrderReleases))]
        public async Task<IHttpActionResult> Add(string orderNo = null, int id = 0)
        {
            return Ok(await GenerateMpOrderRelease(orderNo, id));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<OrderReleases> GenerateMpOrderRelease(string orderNo, int id)
        {
            OrderReleases orderRelease = await this.Find(orderNo, id);

            if (orderRelease == null)
                throw new NothingToProcessException(string.Format("Order No '{0}' with Release No '{1}' could not be found.", orderNo, id));
            else
                orderRelease.GenerateRequirements(true);
                this.Provider?.Update(this.CompId);

            return orderRelease;
        }

        protected virtual async Task<List<OrderReleases>> ProcessEditRequest(dynamic body, string orderNo = null, int id = 0)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            var entityList = new List<OrderReleases>();
            foreach (dynamic item in list)
            {
                var entity = await this.ProcessBodyItem(item, orderNo, id);
 
                this.Provider.Items[0]?.DetailList.Add(entity);
                this.Provider.Items[0]?.MarkAsDirty();

                if (!entityList.Contains(entity))
                    entityList.Add(entity);
            }
            this.Provider?.Update(this.CompId);

            return entityList;
        }

        protected virtual async Task<OrderReleases> ProcessBodyItem(dynamic bodyItem, string orderNo, int id)
        {
            string code = orderNo;
            int release = Convert.ToInt32(bodyItem.ReleaseNo ?? id);

            if (ApiUserSkipped.IsApiUserSkipped(bodyItem.OrderNo) || string.IsNullOrWhiteSpace(bodyItem.OrderNo))
                bodyItem.OrderNo = code;
            else
                code = bodyItem.OrderNo;

            var entity = await this.Find(code, release);

            entity = await this.GenerateMpOrderRelease(code, release);
   
            this.Provider?.Update(this.CompId);

            return entity;
        }

        protected virtual async Task<EntityList<OrderReleases>> Load(string orderNo, int id)
        {
            var list = CurrentOrder?.DetailList;

            if (CurrentOrder == null || CurrentOrder.OrderNo != orderNo)
            {
                var builder = new SqlFilterBuilder<OrderBase.Columns>();
                builder.AppendEquals(OrderBase.Columns.OrderNo, orderNo);
                Provider.Load(this.CompId, new FilterCriteria(builder.ToString(), string.Empty));

                await this.FilterEntityListAsync(Provider.Items);

                if (Provider.Items.Count <= 0)
                    throw new InvalidValueException(string.Format("Order Number '{0}' could not be found.", orderNo));

                CurrentOrder = Provider.Items[0];

                var filteredList = CurrentOrder.DetailList?.FindAll(x => x.ReleaseNo == id);

                await FilterEntityListAsync(filteredList);

                if (filteredList?.Count <= 0)
                    throw new InvalidValueException(string.Format("Release ID '{0}' could not be found.", id));

                list = filteredList;
            }

            return list;
        }

        protected virtual async Task<OrderReleases> Find(string orderNo, int id)
        {
            var list = await Load(orderNo, id);
            return list.Find(x => x.ReleaseNo == id);
        }
        #endregion Helper Methods

        #region Properties
        private OrderProvider Provider { get; } = new OrderProvider();

        protected Order CurrentOrder { get; set; }

        protected OrderReleases CurrentOrderRelease { get; set; }
        #endregion Properties

        #region Fields
        private const string FunctionID = "8F1C5A0F-5A92-4970-831A-09F2C1F242F7";
        #endregion Fields
    }
}

