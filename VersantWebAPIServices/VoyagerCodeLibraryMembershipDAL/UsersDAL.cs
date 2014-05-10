using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Security;

namespace VoyagerCodeLibraryMembershipDAL1
{
    public class UsersDAL
    {
        public static bool ValidateUser(string userName, string password)
        {
            return Membership.ValidateUser(userName, password);
        }

        public static MembershipUser GetUser(string userName)
        {
            return Membership.GetUser(userName);
        }

        public static MembershipUser GetUser(object userdID)
        {
            return Membership.GetUser(userdID);
        }
    }
}
