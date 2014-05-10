using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Models
{
    public class User
    {
        /// <summary>
        /// Initializes a new instance of the User class.
        /// </summary>
        public User(string facilityId, string userName, string password)
        {
            FacilityId = facilityId;
            UserName = userName;
            Password = password;
        }

        public string FacilityId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}