using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

using SystemArts.VersantRN;
using SystemArts.VersantRN.BusinessLogicLayer;
using SystemArts.VersantRN.Data;

using VersantWebAPIServices.Core;
using VersantWebAPIServices.Security;

namespace VersantWebAPIServices.Controllers
{
    [Authorize(Roles = "Resident")]
    public class TimingGroupController : VersantBaseApiController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {
            List<TimingGroup> timingGroups = this.Workspace.TimingGroupCollection.Select(this.Workspace.CurrentUser.LoginFacilityID);
            List<Entity<int, TimingGroupData>> timingGroupsData = this.Workspace.TimingGroupCollection.GetSerializableEntityList(timingGroups);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(timingGroupsData), Encoding.UTF8, "application/json");

            return response;
        }
    }
}
