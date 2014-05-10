using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Models
{
    public struct SubmitData
    {
        public List<ResponseData> Responses { get; set; }
        public DateTime AttemptDate { get; set; }
        public string Comment { get; set; }
    }
}