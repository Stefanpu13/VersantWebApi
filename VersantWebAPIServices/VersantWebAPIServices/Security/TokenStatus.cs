using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VersantWebAPIServices.Security
{
    public static class TokenStatus
    {
        private static List<KeyValuePair<string, ExpirationPolicy>> tokenExpirationStore = new List<KeyValuePair<string, ExpirationPolicy>>();

        public static bool IsExpired(string token, string userID)
        {
            KeyValuePair<string, ExpirationPolicy> tokenEntity = tokenExpirationStore.FirstOrDefault(d => d.Key == token);
            ExpirationPolicy expirationPolicy = tokenEntity.Value;

            if (expirationPolicy == null)
            {
                return true;
            }
            else
            {
                if (DateTime.Now > expirationPolicy.ExpirationTime)
                {
                    tokenExpirationStore.Remove(tokenEntity);
                    return true;
                }
                else
                {
                    if (DateTime.Now > expirationPolicy.ActivityTime)
                    {
                        tokenExpirationStore.Remove(tokenEntity);
                        return true;
                    }
                    else
                    {
                        expirationPolicy.UpdateExpirationPolicy();
                        return false;
                    }
                }
            }
        }

        public static void CreateExpirationPolicy(string token, string userID)
        {
            tokenExpirationStore.RemoveAll(d => d.Value.UserID == userID);
            tokenExpirationStore.Add(new KeyValuePair<string, ExpirationPolicy>(token, ExpirationPolicy.CreateTokenExpirationPolicy(userID)));
        }
    }
}