using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Security
{
    public class ExpirationPolicy
    {
        private static int expirationHours = 24;
        private static int activityHours = 3;

        public string UserID { get; set; }
        public DateTime ExpirationTime { get; set; }
        public DateTime ActivityTime { get; set; }

        private ExpirationPolicy(string userID, DateTime expirationTime, DateTime activityTime)
        {
            this.UserID = userID;
            this.ExpirationTime = expirationTime;
            this.ActivityTime = activityTime;
        }

        public static ExpirationPolicy CreateTokenExpirationPolicy(string userID)
        {
            return new ExpirationPolicy(userID, DateTime.Now.AddHours(expirationHours), DateTime.Now.AddHours(activityHours));
        }

        public void UpdateExpirationPolicy()
        {
            this.ActivityTime = DateTime.Now.AddHours(activityHours);
        }
    }
}