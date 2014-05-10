using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Models
{
    public class ResponseData
    {
        public int QuestionID { get; set; }
        public string RValue { get; set; }
        public int? RValueInt { get; set; }
    }
}