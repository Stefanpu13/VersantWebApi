using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using VersantWebAPIServices.Models;
using System.Net;
using System.Net.Http;
using VersantWebAPIServices.Security;
using VersantWebAPIServices.Core;
using System.Web.Security;
using System.Web;
using SystemArts.VersantRN;
using SystemArts.VersantRN.BusinessLogicLayer;
using Newtonsoft.Json;
using System.Text;
using SystemArts.VersantRN.Data;

namespace VersantWebAPIServices.Controllers
{
    public class UsersController : VersantBaseApiController
    {
        public Status Authenticate(User user)
        {
            if (user == null)
            {
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.Unauthorized, Content = new StringContent("Please provide a valid credentials.") });
            }

            if (IdentityStore.IsValidUser(user))
            {
                Token token = new Token(user.FacilityId, user.UserName, Request.GetClientIP());
                this.Workspace.InitializeUserContext(user.UserName, int.Parse(user.FacilityId));
                string encryptedToken = token.Encrypt();
                
                TokenStatus.CreateExpirationPolicy(encryptedToken, Utils.MakeLoginUserName(user.FacilityId, user.UserName));
                return new Status { Successeded = true, Token = encryptedToken, Message = "Successfully signed in.", UserFullName = this.Workspace.CurrentUser.FullName };
            }
            else
            {
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.Unauthorized, Content = new StringContent("Invalid user name or password.") });
            }
        }

        [HttpGet]
        public HttpResponseMessage Facilities()
        {
            List<Facility> facilities = this.Workspace.FacilityCollection.Select(true, true, "Name");
            if (facilities == null)
            {
                HttpResponseMessage res = new HttpResponseMessage(HttpStatusCode.NotFound);
                res.Content = new StringContent("There was an error getting the facilities");
                throw new HttpResponseException(res);
            }

            List<Models.FacilityData> facilitiesInfo = new List<Models.FacilityData>();
            foreach (Facility facility in facilities)
            {
                facilitiesInfo.Add(new Models.FacilityData { ID = facility.ID, Name = facility.Name });
            }

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(facilitiesInfo), Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet] 
        public HttpResponseMessage GetFilteredResidents(string residentName, int? timingGroupID, int? facilityID)
        {
            int hospitalSystemID = this.Workspace.CurrentUser.HospitalSystemID;

            List<ResidentProfile> residentProfiles = this.Workspace.ResidentProfileCollection.SelectTimingGroupFacilityResidents(true, hospitalSystemID, timingGroupID, facilityID, null);

            List<UserData> residentsToReturn = new List<UserData>();
            foreach (ResidentProfile resident in residentProfiles)
            {
                if (residentName != null)
                {
                    if (!resident.FullName.ToLower().Contains(residentName.ToLower()))
                    {
                        continue;
                    }
                }

                if (!resident.TimingGroupID.HasValue || (timingGroupID.HasValue && resident.TimingGroupID.Value != timingGroupID.Value))
                {
                    continue;
                }

                List<string> timingGroupNames = new List<string>();
                timingGroupNames.Add(resident.TimingGroup.Name);
                
                if (resident.FirstTimingGroupID != resident.TimingGroupID)
                {
                    timingGroupNames.Add(resident.FirstTimingGroup.Name);
                }

                residentsToReturn.Add(new UserData
                {
                    UserID = resident.ID,
                    FullName = resident.FullName,
                    TimingGroupID = resident.TimingGroupID,
                    TimingGroupNames = timingGroupNames
                });
            }

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(residentsToReturn), Encoding.UTF8, "application/json");

            return response;
        }
    }
}
