using Azure.Messaging.EventGrid;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TRAVERSE.Business;
using TRAVERSE.Business.AIP;
using TRAVERSE.Core;

namespace TRAVERSE.Web.API.AIP.Controllers
{
    public class ApiAIPController : ApiControllerBase
    {
        #region Web Methods
        [ApiRoute(FunctionID, 2f, "aip/{eventname}", typeof(EventGridEvent))]
        public async Task<IHttpActionResult> Add([FromBody] dynamic body, string eventname)
        {
            List<AIPActivity> aipActivityList = new List<AIPActivity>();

            AIPActivity activity = new AIPActivity(this.CompId);
            activity.EventId = Guid.NewGuid();
            activity.EntryDate = DateTime.Now;
            activity.EventName = eventname; //Convert.ToString(this.ControllerContext.RouteData.Values["eventname"]);
            activity.ActType = (byte)AIPEnum.ActType.Inbound;
            activity.ActStatus = (byte)AIPEnum.ActStatus.Pending;
            //activity.DocumentReference = string.Format("{0}-{1}", documentRef, Utility.GenerateUniqueId());
            activity.RequestData = JsonConvert.SerializeObject(body, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });
            activity.UserId = Core.ApplicationContext.CurrentUser;
            activity.WrkStnId = Core.ApplicationContext.SessionId;

            this.Provider.Items.Add(activity);

            if (!aipActivityList.Contains(activity))
                aipActivityList.Add(activity);

            await Task.Run(() =>
            {
                this.ValidateEntityList(aipActivityList);
                this.Provider.Update(this.CompId);
            });

            return Ok(aipActivityList);
        }
        #endregion Web Methods

        #region Helper Methods
        protected override void AddPropertyDelegates() { }
        #endregion Helper Methods

        #region Properties
        private const string FunctionID = "35ad288a-5402-42cf-80d4-da6cb6b9e54f";
        protected AIPActivityProvider Provider { get; } = new AIPActivityProvider();
        #endregion Properties
    }
}
