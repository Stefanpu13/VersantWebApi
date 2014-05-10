using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Models
{
    public struct PerformanceCriteriaData
    {
        public string ExternalRef { get; set; }
        public string Name { get; set; }
        public ResponseData Response { get; set; }
    }
}