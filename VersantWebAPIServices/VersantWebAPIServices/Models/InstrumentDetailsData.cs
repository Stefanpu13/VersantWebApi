using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemArts.VersantRN.Data;

namespace VersantWebAPIServices.Models
{
    public class InstrumentDetailsData
    {
        public int ID { get; set; }
        public EvalQuestionData Data { get; set; }

        public InstrumentDetailsData(int id, EvalQuestionData data)
        {
            this.ID = id;
            this.Data = data;
        }
    }

    public class InstrumentDetailsDynamicData : InstrumentDetailsData
    {
        public List<KeyValuePair<string, string>> DynamicData { get; set; }

        public InstrumentDetailsDynamicData(int id, EvalQuestionData data, List<KeyValuePair<string, string>> dynamicData) : base(id, data)
        {
            this.DynamicData = dynamicData;
        }
    }
}