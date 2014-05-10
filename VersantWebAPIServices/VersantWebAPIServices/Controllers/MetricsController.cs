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
    [Authorize(Roles = "Resident")]
    public class MetricsController : VersantBaseApiController
    {
        [HttpGet]
        public HttpResponseMessage Available()
        {
            return this.GetScheduled(MetricType.Available);
        }

        [HttpGet]
        public HttpResponseMessage Upcoming()
        {
            return this.GetScheduled(MetricType.Upcoming);
        }

        [NonAction]
        private HttpResponseMessage GetScheduled(MetricType command)
        {
            int? timingGroupID = this.Workspace.CurrentUser.TimingGroupID;
            int residentId = this.Workspace.CurrentUserID;
            string result = String.Empty;

            MonthlyEvalEventCollection collection = new MonthlyEvalEventCollection();
            IEnumerable<MonthlyEvalEvent> evals = collection.Select(timingGroupID.Value);

            if (command == MetricType.Available)
            {
                evals = evals.Where(item => item.StartDate < DateTime.Now)
                                        .OrderByDescending(item => item.StartDate);
            }
            else if (command == MetricType.Upcoming)
            {
                evals = evals.Where(item => item.StartDate > DateTime.Now)
                                        .OrderBy(item => item.StartDate);
            }
            else
            {
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent("You are using a wrong MetricType!") });
            }

            List<Entity<int, MonthlyEvalEventData>> evalEntities = collection.GetSerializableEntityList(evals.ToList());
            Dictionary<int, int> evalStatuses = new Dictionary<int, int>(evalEntities.Count);

            // determine the Eval Status based on details submissions
            foreach (Entity<int, MonthlyEvalEventData> item in evalEntities)
            {
                MonthlyEvalDetailCollection collectionDetails = new MonthlyEvalDetailCollection();
                List<MonthlyEvalDetail> evalDetails = collectionDetails.Select(timingGroupID.Value, residentId, item.ID);

                int status = 0; // NotStarted
                if (command == MetricType.Upcoming)
                {
                    status = 3; // Locked
                }
                else
                {
                    if (evalDetails.Count > 0)
                    {
                        int submitted = evalDetails.Count(d => d.IsSubmitted == 1);
                        int saved = evalDetails.Count(d => d.IsSaved == 1);
                        if (submitted == evalDetails.Count)
                        {
                            status = 2;     // ALL submitted
                        }
                        else if (saved > 0 || submitted > 0)
                        {
                            status = 1;     // started, but not fully completed
                        }
                    }
                }
                evalStatuses.Add(item.ID, status);
            }
            var data = from e in evalEntities
                       join s in evalStatuses on e.ID equals s.Key
                       select new
                       {
                           ID = e.ID,
                           Data = e.Data,
                           Status = s.Value
                       };
            result = JsonConvert.SerializeObject(data);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage NonScheduled(int timingGroupID)
        {
            int residentID = this.Workspace.CurrentUserID;

            NonScheduledInstrumentCollection collection = this.Workspace.NonScheduledInstrumentCollection;
            List<NonScheduledInstrument> nonSchInstruments = collection.SelectByRole(this.ResidentRoleID, string.Empty, timingGroupID, residentID);

            List<Entity<int, NonScheduledInstrumentData>> nonSchEntities = collection.GetSerializableEntityList(nonSchInstruments);
            Dictionary<int, Tuple<int, int, DateTime?>> nonSchDetails = new Dictionary<int, Tuple<int, int, DateTime?>>(nonSchEntities.Count);

            foreach (Entity<int, NonScheduledInstrumentData> item in nonSchEntities)
            {
                List<QuestionerAnswered> submissions = this.Workspace.QuestionerAnsweredCollection.SelectFiltered(item.Data.QuestionerID, residentID, timingGroupID, this.ResidentRoleID);

                int status = 0;     // Not Started
                int numberOfSubmissions = submissions.Count;
                DateTime? lastSubmissionDate = null;

                QuestionResponseSaveCollection savedResponseCollection = new QuestionResponseSaveCollection();
                List<QuestionResponseSave> saved = savedResponseCollection.Select(residentID, item.Data.QuestionerID, timingGroupID, this.ResidentRoleID);
                int numberOfSaved = saved.Count;

                if (numberOfSubmissions > 0)
                {
                    status = 2; // Submitted

                    foreach (QuestionerAnswered submission in submissions)
                    {
                        if (!lastSubmissionDate.HasValue || lastSubmissionDate < submission.DTCreated)
                        {
                            lastSubmissionDate = submission.DTCreated;
                        }
                    }
                }
                else
                {
                    if (numberOfSaved > 0)
                    {
                        status = 1;
                    }
                    else
                    {
                        status = 0;
                    }
                }
                nonSchDetails.Add(item.ID, new Tuple<int, int, DateTime?>(status, numberOfSubmissions, lastSubmissionDate)); //new NonSchDetails(status, numberOfSubmissions, lastSubmissionDate));
            }
            var nonSchData = from e in nonSchEntities
                             join s in nonSchDetails on e.ID equals s.Key
                             select new
                             {
                                 ID = e.ID,
                                 Data = e.Data,
                                 Status = s.Value.Item1,
                                 NumberOfSubmissions = s.Value.Item2,
                                 LastSubmissionDate = s.Value.Item3
                             };

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(nonSchData), Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage ScheduledInstruments(int instrumentID)
        {
            int? timingGroupID = this.Workspace.CurrentUser.TimingGroupID;
            int residentId = this.Workspace.CurrentUserID;

            MonthlyEvalDetailCollection collection = new MonthlyEvalDetailCollection();
            List<MonthlyEvalDetail> rawData = collection.Select(timingGroupID, residentId, instrumentID);

            List<Entity<int, MonthlyEvalDetailData>> evalDetails = collection.GetSerializableEntityList(rawData)
                .OrderByDescending(d => d.Data.IsSubmitted)
                .ThenByDescending(e => e.Data.IsSaved).ToList();
            Dictionary<int, Tuple<int, bool>> evalDetailStatuses = new Dictionary<int, Tuple<int, bool>>(evalDetails.Count);

            bool firstNonSubmittedMarked = false;

            foreach (Entity<int, MonthlyEvalDetailData> item in evalDetails)
            {
                int status = 0; // Not Started
                bool isLocked = false;

                int submitted = item.Data.IsSubmitted;
                int saved = item.Data.IsSaved;
                
                if (submitted > 0)
                {
                    status = 2;     // submitted
                    isLocked = true;
                }
                else
                {
                    if (saved > 0)
                    {
                        status = 1;     // started, but not fully completed
                        isLocked = false;
                    }
                    else
                    {
                        if (!firstNonSubmittedMarked)
                        {
                            firstNonSubmittedMarked = true; // Raise the flag and unlock the instrument
                            isLocked = false;
                        }
                        else
                        {
                            isLocked = true;
                        }
                    }
                }
                
                evalDetailStatuses.Add(item.ID, new Tuple<int, bool>(status, isLocked));
            }

            var data = from e in evalDetails
                       join s in evalDetailStatuses on e.ID equals s.Key
                       select new
                       {
                           ID = e.ID,
                           Data = e.Data,
                           Status = s.Value.Item1,
                           IsLocked = s.Value.Item2
                       };

            string result = JsonConvert.SerializeObject(data);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage NonScheduledInstruments(int instrumentID, int? timingGroupID)
        {
            int residentID = this.Workspace.CurrentUserID;

            NonScheduledInstrument nonSchInstrument = this.Workspace.NonScheduledInstrumentCollection.GetItemByID(instrumentID);

            List<QuestionerAnswered> rawData = this.Workspace.QuestionerAnsweredCollection.SelectFiltered(nonSchInstrument.QuestionerID, residentID, timingGroupID, this.ResidentRoleID);
            List<Entity<int, QuestionerAnsweredData>> submissions = this.Workspace.QuestionerAnsweredCollection.GetSerializableEntityList(rawData);

            QuestionResponseSaveCollection savedResponseCollection = new QuestionResponseSaveCollection();
            List<QuestionResponseSave> savedRaw = savedResponseCollection.Select(residentID, nonSchInstrument.QuestionerID, timingGroupID, this.ResidentRoleID);
            List<Entity<int, QuestionResponseSaveData>> saved = savedResponseCollection.GetSerializableEntityList(savedRaw);

            var resultData = new
            {
                Name = nonSchInstrument.Name,
                QuestionerID = nonSchInstrument.QuestionerID,
                Submitted = submissions,
                Saved = saved.FirstOrDefault()
            };

            string result = JsonConvert.SerializeObject(resultData);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage ScheduledInstrumentDetails(int questionerID, int eventId)
        {
            int? timingGroupID = this.Workspace.CurrentUser.TimingGroupID;
            int residentID = this.Workspace.CurrentUserID;

            EvalQuestionCollection collection = new EvalQuestionCollection();
            List<EvalQuestion> rawData = collection.SelectByTGAndRole(questionerID, residentID, timingGroupID, this.ResidentRoleID);

            List<Entity<int, EvalQuestionData>> data = collection.GetSerializableEntityList(rawData);
            List<InstrumentDetailsData> instrumentDetailsData = ObjectTypeQuestions.Populate(data);

            DateTime? lastSaved = null;
            QuestionResponseSave savedResponse = (new QuestionResponseSaveCollection()).Select(residentID, questionerID, timingGroupID, this.ResidentRoleID).FirstOrDefault();
            if (savedResponse != null)
            {
                lastSaved = savedResponse.DTCreated;
            }

            var resultData = new
            {
                Questions = instrumentDetailsData,
                LastSaved = lastSaved
            };

            string result = JsonConvert.SerializeObject(resultData);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage NonScheduledInstrumentDetails(int questionerID, int? timingGroupID)
        {
            int residentID = this.Workspace.CurrentUserID;
            if (!timingGroupID.HasValue)
            {
                string message = "timingGroupID parameter is required for NonScheduledInstrumentDetails. Consider using the ScheduledInstrumentDetails action, if you have no specified timing group.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            EvalQuestionCollection collection = new EvalQuestionCollection();
            
            List<EvalQuestion> rawData = collection.SelectByTGAndRole(questionerID, residentID, timingGroupID, this.ResidentRoleID);
            List<Entity<int, EvalQuestionData>> data = collection.GetSerializableEntityList(rawData);
            List<InstrumentDetailsData> instrumentDetailsData = ObjectTypeQuestions.Populate(data);

            DateTime? lastSaved = null;
            QuestionResponseSave savedResponse = (new QuestionResponseSaveCollection()).Select(residentID, questionerID, timingGroupID, this.ResidentRoleID).FirstOrDefault();
            if (savedResponse != null)
            {
                lastSaved = savedResponse.DTCreated;
            }

            var resultData = new
            {
                Questions = instrumentDetailsData,
                LastSaved = lastSaved
            };

            string result = JsonConvert.SerializeObject(resultData);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [HttpPost]
        public HttpResponseMessage SaveForLaterScheduled(int questionerID, List<ResponseData> responseData)
        {
            int? timingGroupID = this.Workspace.CurrentUser.TimingGroupID;
            return SaveForLater(questionerID, timingGroupID, responseData);
        }

        [HttpPost]
        public HttpResponseMessage SaveForLaterNonScheduled(int questionerID, int? timingGroupID, List<ResponseData> responseData)
        {
            if (!timingGroupID.HasValue)
            {
                string message = "timingGroupID parameter is required for SaveForLaterNonScheduled. Consider using the SaveForLaterScheduled action, if you have no specified timing group.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }
            return SaveForLater(questionerID, timingGroupID, responseData);
        }

        [HttpPost]
        public HttpResponseMessage SubmitScheduled(int questionerID, bool? overrideWarning, List<ResponseData> responseData)
        {
            int? timingGroupID = this.Workspace.CurrentUser.TimingGroupID;
            return Submit(questionerID, timingGroupID, overrideWarning, responseData);
        }

        [HttpPost]
        public HttpResponseMessage SubmitNonScheduled(int questionerID, int? timingGroupID, bool? overrideWarning, List<ResponseData> responseData)
        {
            if (!timingGroupID.HasValue)
            {
                string message = "timingGroupID parameter is required for SubmitNonScheduled. Consider using the SubmitScheduled action, if you have no specified timing group.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }
            return Submit(questionerID, timingGroupID, overrideWarning, responseData);
        }
   
        [NonAction]
        private HttpResponseMessage SaveForLater(int questionerID, int? timingGroupID, List<ResponseData> responseData)
        {
            int residentID = this.Workspace.CurrentUserID;
            if (!timingGroupID.HasValue)
            {
                timingGroupID = this.Workspace.CurrentUser.TimingGroupID;
            }

            EvalQuestionCollection collection = new EvalQuestionCollection();
            List<EvalQuestion> list = collection.SelectByTGAndRole(questionerID, residentID, timingGroupID, this.ResidentRoleID);

            HttpStatusCode statusCode = HttpStatusCode.OK;
            string responseMessage = "Save successfully updated!";

            List<QuestionResponseSave> savedResponses = (new QuestionResponseSaveCollection()).Select(residentID, questionerID, timingGroupID, this.ResidentRoleID);
            if (savedResponses.Count == 0)
            {
                statusCode = HttpStatusCode.Created;
                responseMessage = "Save successfully created!";
            }

            for (int i = 0; i < responseData.Count; i++)
            {
                int questionID = responseData[i].QuestionID;
                EvalQuestion question = collection.GetItemByID(questionID);
                if (question.Active == true)
                {
                    if (question.Response == null)
                    {
                        question.Response = new EvalResponse();
                    }
                    question.Response.QID = questionID;
                    question.Response.ExternalRef = question.ExternalRef;
                    question.Response.QuestionerID = questionerID;
                    question.Response.RValue = responseData[i].RValue;
                    question.Response.RValueInt = responseData[i].RValueInt;
                    question.Response.OwnerID = residentID;
                    question.Response.CreatorID = residentID;
                }
            }

            if (collection.CurrentFilter == null)
            {
                collection.CurrentFilter = new EvalQuestionCollection.ResidentQuestionerFilter(questionerID, residentID, null, null);
            }

            collection.Save(timingGroupID, this.ResidentRoleID);

            HttpResponseMessage response = this.Request.CreateResponse(statusCode);
            response.Content = new StringContent(responseMessage);

            return response;
        }

        [NonAction]
        public HttpResponseMessage Submit(int questionerID, int? timingGroupID, bool? overrideWarning, List<ResponseData> responseData)
        {
            int residentID = this.Workspace.CurrentUserID;
            if (!overrideWarning.HasValue)
            {
                overrideWarning = false;
            }

            EvalQuestionCollection collection = new EvalQuestionCollection();
            List<EvalQuestion> list = collection.SelectByTGAndRole(questionerID, residentID, timingGroupID, this.ResidentRoleID);

            for (int i = 0; i < responseData.Count; i++)
            {
                int questionID = responseData[i].QuestionID;
                EvalQuestion question = collection.GetItemByID(questionID);
                if (question.Active == true)
                {
                    if (question.Response == null)
                    {
                        question.Response = new EvalResponse();
                    }
                    question.Response.QID = questionID;
                    question.Response.ExternalRef = question.ExternalRef;
                    question.Response.QuestionerID = questionerID;
                    question.Response.RValue = responseData[i].RValue;
                    question.Response.RValueInt = responseData[i].RValueInt;
                    question.Response.OwnerID = residentID;
                    question.Response.CreatorID = residentID;
                }
            }

            double answeredQuestions = 0;
            int total = 0;
            foreach (EvalQuestion question in collection.AllItemsList())
            {
                if (question.IsRequired)
                {
                    total++;
                    if (question.Response != null)
                    {
                        answeredQuestions++;
                    }
                }
                if (!string.IsNullOrEmpty(question.DisplayDefinition) && question.DisplayDefinition.Contains(@"display_type=""scale"""))
                {
                    total--;
                }
            }

            Questioner questioner = this.Workspace.QuestionerCollection.GetItemByID(questionerID);
            bool suppressWarnings = questioner != null && questioner.QuestionersName.ToLower().Contains("demographics");

            if (collection.CurrentFilter == null)
            {
                collection.CurrentFilter = new EvalQuestionCollection.ResidentQuestionerFilter(questionerID, residentID, null, null);
            }

            HttpStatusCode statusCode = HttpStatusCode.Created;
            string responseMessage = "Questioner submitted successfully";

            if(suppressWarnings || (answeredQuestions / total) > 0.8 || overrideWarning.Value)
            {
                collection.Submit(timingGroupID, this.ResidentRoleID);
            }
            else
            {
                collection.Save(timingGroupID, this.ResidentRoleID);
                statusCode = HttpStatusCode.OK;
                responseMessage = String.Format("({0}/{1} completed) You must complete atleast 80% of the questioner's answers to submit it. Questioner saved. Use the overrideWarning parameter to Submit regardless.", answeredQuestions, total);
            }

            HttpResponseMessage response = this.Request.CreateResponse(statusCode);
            response.Content = new StringContent(responseMessage, Encoding.UTF8, "application/json");

            return response;
        }
    }
}
