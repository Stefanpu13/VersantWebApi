using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Xml;
using SystemArts.VersantRN.Data;
using VersantWebAPIServices.Models;

namespace VersantWebAPIServices.Core
{
    public static class ObjectTypeQuestions
    {
        public static List<InstrumentDetailsData> Populate(List<Entity<int, EvalQuestionData>> questions)
        {
            List<InstrumentDetailsData> instrumentDetailsData = new List<InstrumentDetailsData>();

            foreach (Entity<int, EvalQuestionData> question in questions)
            {
                if (question.Data.DisplayDefinition.Contains("dynamic_data"))
                {
                    XmlDocument displayDefinition = new XmlDocument();
                    displayDefinition.LoadXml(question.Data.DisplayDefinition);

                    XmlNode dynamicDataNode = displayDefinition.SelectSingleNode("/eval_question/dynamic_data");
                    string collectionName = dynamicDataNode.Attributes["typeName"].Value;
                    string collectionMethod = dynamicDataNode.Attributes["selectMethod"].Value;

                    instrumentDetailsData.Add(new InstrumentDetailsDynamicData(question.ID, question.Data, GetDynamicData(collectionName, collectionMethod)));
                }
                else
                {
                    instrumentDetailsData.Add(new InstrumentDetailsData(question.ID, question.Data));
                }
            }

            return instrumentDetailsData;
        }

        public static List<KeyValuePair<string, string>> GetDynamicData(string collectionName, string collectionMethod, params object[] parameters)
        {
            List<KeyValuePair<string, string>> dynamicData = new List<KeyValuePair<string, string>>();

            Type type = Type.GetType(collectionName);
            if (type == null)
            {
                string message = "Could not find correct dynamic data type";
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.NotFound, Content = new StringContent(message) });
            }

            try
            {
                ConstructorInfo ci = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
                object collObject = ci.Invoke(null);

                Type[] types = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    types[i] = parameters[i].GetType();
                }
              
                MethodInfo mi = type.GetMethod(collectionMethod, types);
                

                dynamicData = (List<KeyValuePair<string, string>>)mi.Invoke(collObject, parameters);
            }
            catch (Exception ex)
            {
                string message = ("Error invoking reflection method.");
                throw new HttpResponseException(new HttpResponseMessage() { StatusCode = HttpStatusCode.NotFound, Content = new StringContent(message) });
            }

            return dynamicData;
        }
    }
}