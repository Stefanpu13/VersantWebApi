using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Models
{
    public struct AnsweredCriteriaOptions
    {
        public Dictionary<int, string> Criteria { get; set; }
        public int Value { get; set; }
    }
}