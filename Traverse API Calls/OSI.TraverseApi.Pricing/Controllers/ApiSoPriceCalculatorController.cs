#region Using Directives
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Web.API;
#endregion Using Directives

namespace TRAVERSE.Web.API.SalesOrder
{
    public class ApiSoPriceCalculatorController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "pricecalculator", typeof(SoCalculatePrice))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body)
        {
            return Ok(await ProcessEditRequest(body));
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<IEnumerable<SoCalculatePrice>> ProcessEditRequest(dynamic body)
        {
            object[] list;

            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            var entityList = new SoCalculatePrice[list.Length];
            var current = System.Web.HttpContext.Current;
            Parallel.For(0, list.Length, (index) =>
            {
                System.Web.HttpContext.Current = current;
                entityList[index] = this.ProcessBodyItem(list[index]);
            });

            await this.ValidateEntityListAsync(entityList);

            return entityList;
        }

        protected virtual SoCalculatePrice ProcessBodyItem(dynamic bodyItem)
        {
            var entity = new SoCalculatePrice(this.CompId);
            ((ApiEntityModel)bodyItem).PopulateEntity(entity);
            entity.CalculatePrice();

            return entity;
        }
        #endregion Helper Methods

        #region Properties
        public const string FunctionID = "BC40C238-13D4-446B-B61D-21A68CB9F952";
        #endregion Properties
    }
}
