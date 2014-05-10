using System;
using System.Collections.Generic;
using System.Linq;
using VersantWebAPIServices.Models;
using SystemArts.VersantRN;
using System.Web.Security;

namespace VersantWebAPIServices.Security
{
    public class IdentityStore
    {
        public static bool IsValidUser(User user)
        {
            bool authenticated = Membership.ValidateUser(Utils.MakeLoginUserName(user.FacilityId, user.UserName), user.Password);
            return authenticated;
        }

        public static bool IsValidUserId(string facilityId, string userId)
        {
            return Membership.GetUser(Utils.MakeLoginUserName(facilityId, userId)) != null;
        }

    }
}