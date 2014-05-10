using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemArts.VersantRN.Data;

namespace VersantWebAPIServices.Models
{
    public struct SubGroupData
    {
        public bool IsAttemptAuthorized { get; set; }
        public bool IsCommented { get; set; }
        public PerfRecordQuestion SubGroup { get; set; }
    }
}