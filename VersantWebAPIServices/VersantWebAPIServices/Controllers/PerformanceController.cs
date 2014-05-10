using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

using SystemArts.VersantRN;
using SystemArts.VersantRN.BusinessLogicLayer;
using SystemArts.VersantRN.Data;

using VersantWebAPIServices.Core;
using VersantWebAPIServices.Models;
using VersantWebAPIServices.Security;

namespace VersantWebAPIServices.Controllers
{
    public class PerformanceController : VersantBaseApiController
    {
        [Authorize(Roles = "Resident")]
        [HttpGet]
        public HttpResponseMessage Record()
        {
            UserProfile user = this.Workspace.CurrentUser;

            QuickStats stats = new QuickStats();
            PerfRecordOverview resRecord = stats.GetQuickStats(user.UID, user.TimingGroupID);
            
            PerfRecordOverviewData resRecordData = new PerfRecordOverviewData(resRecord);
            string result = JsonConvert.SerializeObject(resRecordData);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [Authorize(Roles = "Preceptor")]
        [HttpGet]
        public HttpResponseMessage GetResidentRecordAsPreceptor(int residentID, int timingGroupID)
        {
            this.InitializeWorkspace();
            QuickStats stats = new QuickStats();
            PerfRecordOverview resRecord = stats.GetQuickStats(residentID, timingGroupID);

            PerfRecordOverviewData resRecordData = new PerfRecordOverviewData(resRecord);
            string result = JsonConvert.SerializeObject(resRecordData);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }
    }
}
