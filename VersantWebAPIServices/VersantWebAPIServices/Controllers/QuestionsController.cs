using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

using SystemArts.VersantRN.DataAccessLayer;
using SystemArts.VersantRN.BusinessLogicLayer;
using SystemArts.VersantRN.Data;

using VersantWebAPIServices.Security;
using VersantWebAPIServices.Core;
using System.Xml;

namespace VersantWebAPIServices.Controllers
{
    public class QuestionsController : VersantBaseApiController
    {
        [Authorize(Roles = "Preceptor")]       
        public HttpResponseMessage Get ()
        {
            HttpContext.Current.Items.Add("FacilityID", this.Workspace.CurrentUser.LoginFacilityID);

            List<PreceptorQuestions> activeQuestions = this.Workspace.PreceptorQuestionsCollection.Select(true);
            string result = JsonConvert.SerializeObject(this.Workspace.PreceptorQuestionsCollection.GetSerializableEntityList(activeQuestions));

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet]
        [Authorize(Roles = "Resident")]
        public HttpResponseMessage CascadeDropDownClasses(string displayDef,string classType)
        {
            XmlDocument displayDefinition = new XmlDocument();
            displayDefinition.LoadXml(displayDef);

            XmlNode dynamicDataNode = displayDefinition.SelectSingleNode("/eval_question/dynamic_data[2]");
            string collectionName = dynamicDataNode.Attributes["typeName"].Value;
            string collectionMethod = dynamicDataNode.Attributes["selectMethod"].Value;

            UserProfile currentProfile = this.Workspace.CurrentUser;

            List<KeyValuePair<string, string>> list = ObjectTypeQuestions.GetDynamicData(collectionName, collectionMethod, classType, currentProfile);            
            
            string result = JsonConvert.SerializeObject(list);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet]
        [Authorize(Roles = "Resident")]
        public HttpResponseMessage CascadeDropDownTimingGroupUsers(string displayDef,string timingGroupID)
        {
            XmlDocument displayDefinition = new XmlDocument();
            displayDefinition.LoadXml(displayDef);

            XmlNode dynamicDataNode = displayDefinition.SelectSingleNode("/eval_question/dynamic_data[2]");
            string collectionName = dynamicDataNode.Attributes["typeName"].Value;
            string collectionMethod = dynamicDataNode.Attributes["selectMethod"].Value;

            List<KeyValuePair<string, string>> list = ObjectTypeQuestions.GetDynamicData(collectionName, collectionMethod, timingGroupID);

            string result = JsonConvert.SerializeObject(list);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }
    }
}
