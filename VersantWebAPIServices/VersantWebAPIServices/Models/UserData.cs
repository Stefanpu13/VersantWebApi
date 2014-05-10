using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Models
{
    public class UserData
    {
        public int UserID { get; set; }
        public string FullName { get; set; }
        public int? TimingGroupID { get; set; }
        public List<string> TimingGroupNames { get; set; }
    }
}