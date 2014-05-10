using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SystemArts.VersantRN;
using SystemArts.VersantRN.BusinessLogicLayer;
using VersantWebAPIServices.Constants;
using VersantWebAPIServices.Security;

namespace VersantWebAPIServices.Core
{
    public abstract class VersantBaseApiController : ApiController
    {
        private Workspace currentWorkspace = null;

        protected void InitializeWorkspace()
        {
            string userName = string.Empty;
            int loginFacilityID = 0;
            if (this.Request.Headers.Contains("X-Token"))
            {
                string encryptedToken = this.Request.Headers.GetValues("X-Token").First();
                Token token = Token.Decrypt(encryptedToken);
                userName = token.UserId;
                loginFacilityID = int.Parse(token.FacilityId);
            }
            this.currentWorkspace = Workspace.OpenWorkspaceForService(WebApiConstants.GUIDWebApiService, userName, loginFacilityID);
        }

        public Workspace Workspace
        {
            get 
            {
                if (this.currentWorkspace == null)
                {
                    this.InitializeWorkspace();
                }
                return this.currentWorkspace; 
            }
        }

        public int ResidentRoleID
        {
            get { return this.Workspace.RoleType.GetID("Resident"); }
        }

        [HttpOptions]
        public HttpResponseMessage Options()
        {
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
    }
}