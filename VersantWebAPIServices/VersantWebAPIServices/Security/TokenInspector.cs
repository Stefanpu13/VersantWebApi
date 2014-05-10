using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using VersantWebAPIServices.Core;
using System.Security.Principal;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Security;
using SystemArts.VersantRN;

namespace VersantWebAPIServices.Security
{
    public class TokenInspector : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const string TOKEN_NAME = "X-Token";

            if (request.Method.Method != "OPTIONS")
            {
                if (request.Headers.Contains(TOKEN_NAME))
                {
                    string encryptedToken = request.Headers.GetValues(TOKEN_NAME).First();
                    try
                    {
                        Token token = Token.Decrypt(encryptedToken);
                        bool isValidUserId = IdentityStore.IsValidUserId(token.FacilityId, token.UserId);
                        bool requestIPMatchesTokenIP = token.IP.Equals(request.GetClientIP());

                        if (!isValidUserId || !requestIPMatchesTokenIP)
                        {
                            HttpResponseMessage reply = request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid indentity or client machine.");
                            return Task.FromResult(reply);
                        }

                        var loginUserName = Utils.MakeLoginUserName(token.FacilityId, token.UserId);

                        bool isTokenExpired = TokenStatus.IsExpired(encryptedToken, loginUserName);
                        if (isTokenExpired)
                        {
                            HttpResponseMessage reply = request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Token expired.");
                            return Task.FromResult(reply);
                        }

                        var identity = new GenericIdentity(loginUserName);
                        var userRoles = Roles.GetRolesForUser(loginUserName);

                        Thread.CurrentPrincipal = new GenericPrincipal(identity, userRoles);
                        if (HttpContext.Current != null)
                        {
                            HttpContext.Current.User = Thread.CurrentPrincipal;
                        }
                    }
                    catch (Exception ex)
                    {
                        HttpResponseMessage reply = request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid token.");
                        return Task.FromResult(reply);
                    }
                }
                else
                {
                    HttpResponseMessage reply = request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Request is missing authorization token.");
                    return Task.FromResult(reply);
                }
            }
            else 
            {
                HttpResponseMessage reply = request.CreateResponse(HttpStatusCode.OK);
                return Task.FromResult(reply);
            }

            return base.SendAsync(request, cancellationToken);
        }

    }
}