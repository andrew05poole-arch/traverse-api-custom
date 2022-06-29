using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TRAVERSE.Business.API;
using TRAVERSE.Business;
using TRAVERSE.Business.Pricing;
using TRAVERSE.Core;

namespace TRAVERSE.Web.API
{
    public class ApiSamplerController : ApiControllerBase
    {
        #region Constructors
        public ApiSamplerController()
        {

        }
        #endregion Constructors

        #region Web Methods
        [ApiRoute(FunctionID, 2f, "pricestruct", typeof(PriceStructureHeader))]
        [ApiRoute(FunctionID, 2f, "pricestruct/{id?}", typeof(PriceStructureHeader))]
        public async Task<IHttpActionResult> Get(string id = null)
        {
            return Ok(await LoadList(id));
        }

        [ApiRoute(FunctionID, 2f, "pricestruct", typeof(PriceStructureHeader))]
        [ApiRoute(FunctionID, 2f, "pricestruct/{id?}", typeof(PriceStructureHeader))]
        public async Task<IHttpActionResult> Put([FromBody] dynamic body, string id = null)
        {
            List<PriceStructureHeader> headers = new List<PriceStructureHeader>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            foreach (dynamic item in list)
            {
                var header = await UpdateHeader(item, id);
                if (!headers.Contains(header))
                    headers.Add(header);
            }

            await ValidateEntityList(headers);
            Provider.Update(CompId);

            return Ok(headers);
        }

        [ApiRoute(DetailFunctionID, 2f, "pricestruct/{id}/detail/{customerLevel?}", typeof(PriceStructureDetail))]
        public async Task<IHttpActionResult> GetDetail(string id, string customerLevel = null)
        {
            var list = await LoadList(id);
            if (list.Count == 0)
                throw new InvalidValueException("Price ID does not exist");

            var header = list[0];

            //Filter the child records
            FilterEntityList(header.DetailRecords);

            //Return the user's list from the filtered records based on if they selected a specific customer level or not
            if (string.IsNullOrEmpty(customerLevel))
                return Ok(header.DetailRecords);
            else
                return Ok(header.DetailRecords.FindAll(d => d.CustLevel.Equals(customerLevel, StringComparison.OrdinalIgnoreCase)));
        }

        [ApiRoute(DetailFunctionID, 2f, "pricestruct/{id}/detail/{customerLevel?}", typeof(PriceStructureDetail))]
        public async Task<IHttpActionResult> PutDetail([FromBody] dynamic body, string id, string customerLevel = null)
        {

            List<PriceStructureDetail> details = new List<PriceStructureDetail>();
            object[] list = null;
            if (body is object[])
                list = body;
            else
                list = new object[1] { body };

            if (list.Length > 1 && !string.IsNullOrEmpty(id))
                throw new InvalidValueException("Call is ambiguous. ID is provided along with more than one record.");

            var headerList = await LoadList(id);

            if (headerList.Count == 0)
                throw new InvalidValueException("Price ID does not exist");

            var header = headerList[0];

            //Filter the child records
            FilterEntityList(header.DetailRecords);

            foreach (dynamic item in list)
            {
                var detail = await UpdateDetail(header, item, customerLevel);
                if (!details.Contains(detail))
                    details.Add(detail);
            }

            await ValidateEntityList(details);
            Provider.Update(CompId);
            return Ok(details);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }

        protected virtual async Task<EntityList<PriceStructureHeader>> Load(string id)
        {
            SqlFilterBuilder<PriceStructureHeaderBase.Columns> builder = new SqlFilterBuilder<PriceStructureHeaderBase.Columns>();
            if (!string.IsNullOrEmpty(id))
                builder.AppendEquals(PriceStructureHeaderBase.Columns.PriceId, id);

            //These provider entries are required for the paging to work properly
            var list = await Provider.Load<PriceStructureHeader>(CompId, new FilterCriteria(builder.ToString(), ""), PageNumber, PageSize);
                
            //This Filter line is required so that the user does not receive data not intended for them to see
            await FilterEntityList(list);
            return list;
        }

        private async Task<PriceStructureHeader> Find(string id)
        {
            //Check our provider first; if not found, use load method to load single instance
            var header = Provider.Items.Find(PriceStructureHeaderBase.Columns.PriceId, id);
            if (header == null)
                header = (await Load(id))?[0];
            
            return header;
        }

        private async Task<PriceStructureHeader> UpdateHeader(dynamic item, string id)
        {
            PriceStructureHeader header = await Find(item.PriceId ?? id);
            if (header == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.PriceId ?? id));

            //Preferred method of populating an entity, when entity has no children or we are not processing the children
            //((ApiEntityModel)item).PopulateEntity(header);
            //Preferred method when we are processing children
            await ((ApiEntityModel)item).PopulateEntityAsync(header, LoadPriceStructureDetail);

            //Optional method of populating an entity
            //header.Description = item.Description ?? header.Description;
            //header.DfltAdjBase = item.DfltAdjBase ?? header.DfltAdjBase;
            //header.DfltAdjType = item.DfltAdjType ?? header.DfltAdjType;
            //header.DfltAdjAmt = item.DfltAdjAmt ?? header.DfltAdjAmt;

            return header;
        }

        private async Task<PriceStructureDetail> UpdateDetail(PriceStructureHeader header, dynamic item, string customerLevel)
        {
            PriceStructureDetail detail = await Find(item.CustomerLevel ?? customerLevel);
            if (header == null)
                throw new InvalidValueException(string.Format("The id '{0}' could not be found", item.CustomerLevel ?? customerLevel));

            //Unlike the header, there are no child descendants so we do not need to try to populate them
            //Apply any necessary Property Changed events on the detail prior to this so the event is triggered properly
            await ((ApiEntityModel)item).PopulateEntityAsync(detail);

            return detail;
        }

        protected virtual object LoadPriceStructureDetail(ApiChildRecordArgs args)
        {
            if (args.PropertyName == "DetailRecords")
            {
                //Optional method to filter the details, if needed. Keep in mind that the using the validation process will filter and validate the details as well
                //FilterEntityList(((PriceStructureHeader)args.ParentObject).DetailRecords, DetailFunctionID);

                return ((PriceStructureHeader)args.ParentObject).DetailRecords.Find(PriceStructureDetailBase.Columns.CustLevel, args.ItemModel.CustLevel);
            }

            return null;
        }
        #endregion Helper Methods

        #region Properties
        protected virtual PriceStructureHeaderProvider Provider { get; } = new PriceStructureHeaderProvider();
        private const string FunctionID = "709c38e7-4eee-4516-9ff7-9eedafecfc8e";
        private const string DetailFunctionID = "8A96E640-D2B7-4197-949D-BA1407532F7F";
        #endregion Properties

        #region Fields
        private PriceStructureHeaderProvider _provider;
        #endregion Fields
    }
}