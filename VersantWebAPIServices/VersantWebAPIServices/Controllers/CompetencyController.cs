using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Security;
using SystemArts.VersantRN;
using SystemArts.VersantRN.BusinessLogicLayer;
using SystemArts.VersantRN.Data;
using VersantWebAPIServices.Constants;
using VersantWebAPIServices.Core;
using VersantWebAPIServices.Models;
using VersantWebAPIServices.Security;

namespace VersantWebAPIServices.Controllers
{
    public class CompetencyController : VersantBaseApiController
    {
        [Authorize(Roles = "Resident")]
        [HttpGet]
        public HttpResponseMessage GetCompetencyAsResident(string type)
        {
            int residentID = this.Workspace.CurrentUserID;
            int? timingGroupID = this.Workspace.CurrentUser.TimingGroupID;
            return this.GetCompetency(type, residentID, timingGroupID);
        }

        [Authorize(Roles = "Preceptor")]
        [HttpGet]
        public HttpResponseMessage GetCompetencyAsPreceptor(string type, int residentID, int timingGroupID)
        {
            this.InitializeWorkspace();
            return this.GetCompetency(type, residentID, timingGroupID);
        }

        [NonAction]
        public HttpResponseMessage GetCompetency(string type, int residentID, int? timingGroupID)
        {
            TimingGroup timingGroup = new TimingGroupCollection().GetItemByID(timingGroupID);
            if (timingGroup == null)
            {
                string message = "Non existing timing group.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }    

            PerfRecordOverview resRecord = new QuickStats().GetQuickStats(residentID, timingGroupID);

            StatsRawData data = resRecord.RawData;
            string result = String.Empty;

            switch (type)
            {
                case "c":
                    {
                        List<SubGroupData> questionGroupsToReturn = this.BuildQuestionGroupsToReturn(data.CoreQuestionGroups, residentID);

                        var returnData = new
                        {
                            name = "Core",
                            timingGroupName = timingGroup.Name,
                            questionsOffered = resRecord.CompetencyCoreOffered,
                            questionsCompleted = resRecord.CompetencyCoreCompleted,
                            percentCompleted = resRecord.CompetencyCorePerc,
                            questionGroups = questionGroupsToReturn
                        };

                        result = JsonConvert.SerializeObject(returnData);
                        break;
                    }
                case "h":
                    {
                        List<SubGroupData> questionGroupsToReturn = this.BuildQuestionGroupsToReturn(data.HospitalSystemQuestionGroups, residentID);

                        var returnData = new
                        {
                            name = "Health System",
                            timingGroupName = timingGroup.Name,
                            questionsOffered = resRecord.CompetencyHospitalSystemOffered,
                            questionsCompleted = resRecord.CompetencyHospitalSystemCompleted,
                            percentCompleted = resRecord.CompetencyHospitalSystemPerc,
                            questionGroups = questionGroupsToReturn
                        };

                        result = JsonConvert.SerializeObject(returnData);
                        break;
                    }
                case "f":
                    {
                        List<SubGroupData> questionGroupsToReturn = this.BuildQuestionGroupsToReturn(data.FacilityQuestionGroups, residentID);

                        var returnData = new
                        {
                            name = "Facility",
                            timingGroupName = timingGroup.Name,
                            questionsOffered = resRecord.CompetencyFacilityOffered,
                            questionsCompleted = resRecord.CompetencyFacilityCompleted,
                            percentCompleted = resRecord.CompetencyFacilityPerc,
                            questionGroups = questionGroupsToReturn
                        };

                        result = JsonConvert.SerializeObject(returnData);
                        break;
                    }
                case "u":
                    {
                        List<SubGroupData> questionGroupsToReturn = this.BuildQuestionGroupsToReturn(data.UnitQuestionGroups, residentID);

                        var returnData = new
                        {
                            name = "Department Specific",
                            timingGroupName = timingGroup.Name,
                            questionsOffered = resRecord.CompetencyUnitOffered,
                            questionsCompleted = resRecord.CompetencyUnitCompleted,
                            percentCompleted = resRecord.CompetencyUnitPerc,
                            questionGroups = questionGroupsToReturn
                        };

                        result = JsonConvert.SerializeObject(returnData);
                        break;
                    }
                case "s":
                    {
                        List<SubGroupData> questionGroupsToReturn = this.BuildQuestionGroupsToReturn(data.SpecialtyQuestionGroups, residentID);

                        var returnData = new
                        {
                            name = "Specialty",
                            timingGroupName = timingGroup.Name,
                            questionsOffered = resRecord.CompetencySpecialtyOffered,
                            questionsCompleted = resRecord.CompetencySpecialtyCompleted,
                            percentCompleted = resRecord.CompetencySpecialtyPerc,
                            questionGroups = questionGroupsToReturn
                        };

                        result = JsonConvert.SerializeObject(returnData);
                        break;
                    }
                default:
                    {
                        string message = "Query type parameter value not allowed! Allowed values are: (\"c\", \"h\", \"f\", \"u\", \"s\")";
                        throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
                    }
            }

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [Authorize(Roles = "Resident")]
        [HttpGet]
        public HttpResponseMessage MakeSelfAssessment(int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID)
        {
            int residentID = this.Workspace.CurrentUserID;
            
            List<SelfAssess> selfAssessList = (new SelfAssessCollection()).SelectFiltered(residentID, questionerID, groupID, subGroupID, employeeCompetencyRecordID);
            if (selfAssessList.Count > 0)
            {
                string message = "Self assessment already submitted for this competency.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            var selfAssessment = new
            {
                Title = this.GetSubGroupTitle(subGroupID),
                SelfAssessList = this.GetBlankCriteriaOptions(),
                StandartPerformanceCriteria = this.GetBlankPerformanceCriteriaList(questionerID, residentID, groupID, subGroupID)
            };

            string result = JsonConvert.SerializeObject(selfAssessment);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [Authorize(Roles = "Resident")]
        [HttpPost]
        public HttpResponseMessage SubmitSelfAssessment(int score, int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID)
        {
            int residentID = this.Workspace.CurrentUserID;

            if (score < 1 || score > 4)
            {
                string message = "The score parameter is out of range (1, 4).";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            try
            {
                SelfAssess selfAssess = new SelfAssess();
                selfAssess.Score = score;
                selfAssess.ResID = residentID;
                selfAssess.SubGroupID = subGroupID;
                selfAssess.QGroupID = groupID;
                selfAssess.QuestionerID = questionerID;
                selfAssess.EmployeeCompetencyRecordID = employeeCompetencyRecordID;
                selfAssess.Update();
            }
            catch (Exception ex)
            {
                string message = "Self assess submit failed.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("Self assess submitted successfully.");

            return response;
        }

        [Authorize(Roles = "Preceptor")]
        [HttpDelete]
        public HttpResponseMessage ResetSelfAssessment(int selfAssessID)
        {
            try
            {
                bool deleted = new SelfAssessCollection().Delete(selfAssessID);
                if (deleted == false)
                {
                    throw new InvalidOperationException();
                }
            }
            catch (Exception e)
            {
                string message = "Self assess deletion failed.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("The self assess deleted successfully.");

            return response;
        }

        [Authorize(Roles = "Preceptor")]
        [HttpGet]
        public HttpResponseMessage CompetencyAttemptCreate(int residentID, int timingGroupID, int subGroupID, int groupID, int questionerID)
        {
            bool isAttemptAuthorized = QuickStats.IsAttemptAuthorized(residentID, subGroupID, groupID, questionerID);
            int maxNumberOfAttempts = isAttemptAuthorized ? 4 : 3;

            List<Attempt> attempts = (new AttemptCollection()).Select(questionerID, groupID, subGroupID, residentID);
            int numberOfAttempts = attempts.Count;

            if (numberOfAttempts < maxNumberOfAttempts)
            {
                return BlankFormAsPreceptor(residentID, numberOfAttempts + 1, subGroupID, groupID, questionerID, null);
            }
            else
            {
                return this.GetSpecificAttempt(residentID, numberOfAttempts, subGroupID, groupID, questionerID, null, attempts);
            }
        }

        [Authorize(Roles = "Resident")]
        [HttpGet]
        public HttpResponseMessage GetLastAttemptAsResident(int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID)
        {
            int residentID = this.Workspace.CurrentUserID;
            List<Attempt> attempts = (new AttemptCollection()).Select(questionerID, groupID, subGroupID, residentID);
            if (attempts.Count == 0)
            {
                return BlankFormAsResident(subGroupID, groupID, questionerID, employeeCompetencyRecordID);
            }
            else
            {
                int lastAttemptNumber = attempts.Max(d => d.AttemptNum);
                return this.GetSpecificAttempt(residentID, lastAttemptNumber, subGroupID, groupID, questionerID, employeeCompetencyRecordID, attempts);
            }
        }

        [Authorize(Roles = "Preceptor")]
        [HttpPost]
        public HttpResponseMessage SubmitAttempt(int score, SubmitData submitData, int residentID, int subGroupID, int groupID, int questionerID)
        {
            int preceptorID = this.Workspace.CurrentUserID;

            List<ResponseData> responses = submitData.Responses;
            DateTime attemptDate = submitData.AttemptDate;
            string commentText = submitData.Comment;

            if (attemptDate > DateTime.Now)
            {
                string message = "A future Attempt date is not allowed.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            if (score < 1 || score > 4)
            {
                string message = "Score can only be 1, 2, 3 or 4.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            bool passingScore = score == 3 || score == 4 ? true : false;

            foreach (ResponseData response in responses)
            {
                if (!response.RValueInt.HasValue && response.RValue == null)
                {
                    string message = "Form incomplete! Please complete the form, before submitting.";
                    throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
                }

                if (passingScore)
                {
                    if (response.RValue == "2" || response.RValueInt == 2) // Option No
                    {
                        string message = "Overall score cannot be marked 3 or 4 if any competency criteria are marked No!";
                        throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
                    }
                }
            }

            AttemptCollection attemptCollection = new AttemptCollection();
            Attempt newAttempt = attemptCollection.CreateNew();

            newAttempt.Score = score;

            List<Attempt> attempts = attemptCollection.Select(questionerID, groupID, subGroupID, residentID);
            int attemptNum = 1;
            if (attempts != null)
            {
                if (attempts.Count > 0)
                    attemptNum = (attempts[attempts.Count - 1].AttemptNum + 1);
            }

            newAttempt.AttemptNum = attemptNum;
            newAttempt.CompletedBySimulation = false; // chkSim.Checked;
            newAttempt.CreatorID = preceptorID;
            newAttempt.ResID = residentID;
            newAttempt.DTSubmitted = attemptDate;
            newAttempt.QuestionerID = questionerID;
            newAttempt.QGroupID = groupID;
            newAttempt.SubGroupID = subGroupID;

            if (commentText != null)
            {
                ResponseCommentCollection responseCommentCollection = new ResponseCommentCollection();
                ResponseComment responseComment = responseCommentCollection.CreateNew();

                responseComment.Comment = commentText;
                responseComment.OwnerID = residentID;
                responseComment.CreatorID = preceptorID;
                responseComment.DTCreated = attemptDate;
                responseComment.QuestionerID = questionerID;
                responseComment.QGroupID = groupID;
                responseComment.SubGroupID = subGroupID;
                responseComment.attempt = attemptNum;

                responseCommentCollection.Update(responseComment);
            }
            
            EvalQuestionCollection evalQuestionCollection = new EvalQuestionCollection();
            List<EvalQuestion> questions = evalQuestionCollection.Select(questionerID, residentID, groupID, subGroupID);

            newAttempt.Questions = evalQuestionCollection;

            foreach (var question in questions)
            {
                int questionID = question.ID;
                ResponseData responseData = responses.FirstOrDefault(d => d.QuestionID == questionID);
                if (responseData != null)
                {
                    EvalResponse response = new EvalResponse();

                    response.QID = questionID;
                    response.ExternalRef = question.ExternalRef;
                    response.QuestionerID = questionerID;
                    response.RValue = responseData.RValue;
                    response.RValueInt = responseData.RValueInt;
                    response.OwnerID = residentID;
                    response.CreatorID = preceptorID;

                    question.Response = response;
                }
            }

            try
            {
                attemptCollection.Update(newAttempt);
            }
            catch (Exception ex)
            {
                string message = "Error submitting attempt.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            HttpResponseMessage httpResponse = this.Request.CreateResponse(HttpStatusCode.Created);
            string contentMessage = String.Format("Attempt #{0} submitted sucessfully!", attemptNum);
            httpResponse.Content = new StringContent(contentMessage);

            return httpResponse;
        }
        
        [Authorize(Roles = "Resident")]
        [HttpGet]
        public HttpResponseMessage BlankFormAsResident(int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID)
        {
            int residentID = this.Workspace.CurrentUserID;

            string title = this.GetSubGroupTitle(subGroupID);
            List<PerformanceCriteriaData> performanceCriteriaList = this.GetBlankPerformanceCriteriaList(questionerID, residentID, groupID, subGroupID);
            Dictionary<int, string> criteriaOptions = this.GetBlankCriteriaOptions();

            var blankForm = new
            {
                Title = title,
                StandartPerformanceCriteria = performanceCriteriaList,
                SelfAssessList = criteriaOptions
            };

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(blankForm), Encoding.UTF8, "application/json");

            return response;
        }

        [NonAction]
        private HttpResponseMessage BlankFormAsPreceptor(int residentID, int newAttemptNumber, int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID)
        {
            string title = this.GetSubGroupTitle(subGroupID);
            List<PerformanceCriteriaData> performanceCriteriaList = this.GetBlankPerformanceCriteriaList(questionerID, residentID, groupID, subGroupID);
            Dictionary<int, string> criteriaOptions = this.GetBlankCriteriaOptions();

            var blankForm = new
            {
                Title = title,
                AttemptNumber = newAttemptNumber,
                StandartPerformanceCriteria = performanceCriteriaList,
                RecordedDate = DateTime.Now,
                SelfAssessList = criteriaOptions
            };

            string result = JsonConvert.SerializeObject(blankForm);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }


        [Authorize(Roles = "Resident")]
        [HttpGet]
        public HttpResponseMessage GetSpecificAttemptAsResident(int attemptNumber, int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID)
        {
            int residentID = this.Workspace.CurrentUserID;
            return this.GetSpecificAttempt(residentID, attemptNumber, subGroupID, groupID, questionerID, employeeCompetencyRecordID, null);
        }

        [Authorize(Roles = "Preceptor")]
        [HttpGet]
        public HttpResponseMessage GetSpecificAttemptAsPreceptor(int residentID, int attemptNumber, int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID)
        {  
            return this.GetSpecificAttempt(residentID, attemptNumber, subGroupID, groupID, questionerID, employeeCompetencyRecordID, null);
        }

        [NonAction]
        private HttpResponseMessage GetSpecificAttempt(int residentID, int attemptNumber, int subGroupID, int groupID, int questionerID, int? employeeCompetencyRecordID, List<Attempt> attempts)
        {
            this.InitializeWorkspace();
            if (attempts == null)
            {
                attempts = (new AttemptCollection()).Select(questionerID, groupID, subGroupID, residentID);
            }

            Attempt specificAttempt = attempts.Find(d => d.AttemptNum == attemptNumber);

            if (specificAttempt == null)
            {
                string message = "Incorrect attempt number.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            string title = this.GetSubGroupTitle(subGroupID);
            AnsweredCriteriaOptions answeredCriteriaOptions = this.GetAnsweredCriteriaOptions(specificAttempt, questionerID, groupID, subGroupID, residentID);
            List<PerformanceCriteriaData> specificPerformanceCriteriaList = this.GetSpecificPerformanceCriteriaList(specificAttempt, questionerID, residentID, groupID, subGroupID);
            List<string> comments = this.GetComments(questionerID, residentID, groupID, subGroupID);

            var resultData = new
            {
                TotalNumberOfAttempts = attempts.Count,
                CurrentAttempt = specificAttempt.AttemptNum,
                Title = title,
                AttemptDate = specificAttempt.DTCreated,
                RecordedDate = specificAttempt.DTSubmitted,
                PerformanceCriteriaList = specificPerformanceCriteriaList,
                OverallEvaluation = answeredCriteriaOptions,
                AdditionalComments = comments
            };

            string result = JsonConvert.SerializeObject(resultData);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");

            return response;
        }

        [Authorize(Roles = "Preceptor")]
        [HttpGet]
        public HttpResponseMessage QuickComments(int questionerID)
        {
            Questioner questioner = this.Workspace.QuestionerCollection.GetItemByID(questionerID);
            if (questioner == null)
            {
                string message = "Wrong questionerID.";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(message) });
            }

            Facility facility = this.Workspace.FacilityCollection.GetItemByID(this.Workspace.CurrentUser.LoginFacilityID);

            QuestionerPredefinedCommentCollection questionerPredifinedCollection = new QuestionerPredefinedCommentCollection();
            List<QuestionerPredefinedComment> predifinedComments = questionerPredifinedCollection.SelectWithSystemWide(questioner.ID, questioner.QuestionerTypeID, facility.HospitalSystemID, facility.ID, null);

            List<Entity<int, QuestionerPredefinedCommentData>> commentsData = questionerPredifinedCollection.GetSerializableEntityList(predifinedComments);

            string jsonResult = JsonConvert.SerializeObject(commentsData);

            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(jsonResult, Encoding.UTF8, "application/json");

            return response;
        }

        [NonAction]
        private List<SubGroupData> BuildQuestionGroupsToReturn(List<PerfRecordQuestion> questionGroups, int residentID)
        {
            Dictionary<int, Tuple<bool, bool>> statuses = new Dictionary<int, Tuple<bool, bool>>(questionGroups.Count);
            ResponseCommentCollection comments = new ResponseCommentCollection();

            foreach (PerfRecordQuestion group in questionGroups)
            {
                bool isAttemptAuthorized = QuickStats.IsAttemptAuthorized(residentID, group.SubGroupID, group.GroupID, group.QuestionerID);

                if (comments.Select(group.QuestionerID, group.GroupID, group.SubGroupID, residentID).Count > 0)
                {
                    statuses.Add(group.SubGroupID, new Tuple<bool, bool>(isAttemptAuthorized, true));
                }
                else
                {
                    statuses.Add(group.SubGroupID, new Tuple<bool, bool>(isAttemptAuthorized, false));
                }
            }

            IEnumerable<SubGroupData> questionGroupsToReturn = from qg in questionGroups
                                         join s in statuses on qg.SubGroupID equals s.Key
                                         select new SubGroupData
                                         {
                                             IsAttemptAuthorized = s.Value.Item1,
                                             IsCommented = s.Value.Item2,
                                             SubGroup = qg
                                         };

            return questionGroupsToReturn.ToList();
        }

        [NonAction]
        private string GetSubGroupTitle(int subGroupID)
        {
            string title = String.Empty;
            QuestionSubGroup questionSubGroup = new QuestionSubGroup(subGroupID);
            if (questionSubGroup != null)
            {
                title = questionSubGroup.SubGroupName_Text;
            }
            return title;
        }

        [NonAction]
        private Dictionary<int, string> GetBlankCriteriaOptions()
        {
            int numberOfCriteriaOptions = 4;
            Dictionary<int, string> criteriaOptions = new Dictionary<int, string>(numberOfCriteriaOptions);
            criteriaOptions.Add(4, "4 - Can perform this skill without supervision and with initiative and adaptability to problem situations.");
            criteriaOptions.Add(3, "3 - Can perform this skill satisfactorily.");
            criteriaOptions.Add(2, "2 - Can perform this skill satisfactorily but requires assistance and/or supervision.");
            criteriaOptions.Add(1, "1 - Unable to perform this skill.");
            return criteriaOptions;
        }

        [NonAction]
        private AnsweredCriteriaOptions GetAnsweredCriteriaOptions(Attempt specificAttempt, int questionerID, int groupID, int subGroupID, int residentID)
        {
            Dictionary<int, string> criteriaOptions = this.GetBlankCriteriaOptions();

            AnsweredCriteriaOptions answeredCriteriaOptions = new AnsweredCriteriaOptions
            {
                Criteria = criteriaOptions,
                Value = specificAttempt.Score
            };

            return answeredCriteriaOptions;
        }

        [NonAction]
        private List<PerformanceCriteriaData> GetBlankPerformanceCriteriaList(int questionerID, int residentID, int groupID, int subGroupID)
        {
            EvalQuestionCollection evalQuestionCollection = new EvalQuestionCollection();
            List<EvalQuestion> questions = evalQuestionCollection.Select(questionerID, residentID, groupID, subGroupID);

            List<PerformanceCriteriaData> performanceCriteriaList = new List<PerformanceCriteriaData>(questions.Count); 
            foreach (EvalQuestion question in questions)
            {
                performanceCriteriaList.Add(new PerformanceCriteriaData
                { 
                    ExternalRef = question.ExternalRef, 
                    Name = question.Name,
                    Response = null
                });
            }

            return performanceCriteriaList;
        }

        [NonAction]
        private List<PerformanceCriteriaData> GetSpecificPerformanceCriteriaList(Attempt specificAttempt, int questionerID, int residentID, int groupID, int subGroupID)
        {
            EvalQuestionCollection evalQuestionCollection = new EvalQuestionCollection();
            List<EvalQuestion> questions = evalQuestionCollection.Select(questionerID, residentID, groupID, subGroupID, specificAttempt.ID, specificAttempt.CreatorID);

            List<PerformanceCriteriaData> specificPerformanceCriteriaList = new List<PerformanceCriteriaData>(questions.Count);
            foreach (EvalQuestion question in questions)
            {
                if (question.Response != null)
                {
                    ResponseData response = new ResponseData
                    {
                        QuestionID = question.ID,
                        RValue = question.Response.RValue,
                        RValueInt = question.Response.RValueInt
                    };

                    specificPerformanceCriteriaList.Add(new PerformanceCriteriaData
                    {
                        ExternalRef = question.ExternalRef,
                        Name = question.Name,
                        Response = response
                    });
                }
                else
                {
                    specificPerformanceCriteriaList.Add(new PerformanceCriteriaData
                    {
                        ExternalRef = question.ExternalRef,
                        Name = question.Name,
                        Response = null
                    });
                }                
            }

            return specificPerformanceCriteriaList;
        }

        [NonAction]
        private List<string> GetComments(int questionerID, int residentID, int groupID, int subGroupID)
        {
            ResponseCommentCollection responseCommentCollection = new ResponseCommentCollection();
            List<ResponseComment> responseComments = responseCommentCollection.Select(questionerID, groupID, subGroupID, residentID);

            if (responseComments != null)
            {
                responseComments.OrderByDescending(d => d.DTCreated);

                List<string> comments = new List<string>(responseComments.Count);
                foreach (ResponseComment responseComment in responseComments)
                {
                    comments.Add(responseComment.Comment);
                }

                return comments;
            }
            else
            {
                return null;
            }
        }
    }
}
