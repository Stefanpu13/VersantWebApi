using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using VersantWebAPIServices.Core;

namespace VersantWebAPIServices.Security
{
    public class Token
    {
        public Token(string facilityId, string userId, string fromIP)
        {
            FacilityId = facilityId;
            UserId = userId;
            IP = fromIP;
        }

        public string FacilityId { get; private set; }
        public string UserId { get; private set; }
        public string IP { get; private set; }

        public string Encrypt()
        {
            CryptographyHelper cryptographyHelper = new CryptographyHelper();
            X509Certificate2 certificate = cryptographyHelper.GetX509Certificate("CN=WebAPI-Token");
            return cryptographyHelper.Encrypt(certificate, this.ToString());
        }

        public override string ToString()
        {
            return String.Format("FacilityId={0};UserId={1};IP={2}", this.FacilityId, this.UserId, this.IP);
        }

        public static Token Decrypt(string encryptedToken)
        {
            CryptographyHelper cryptographyHelper = new CryptographyHelper();
            X509Certificate2 certificate = cryptographyHelper.GetX509Certificate("CN=WebAPI-Token");
            string decrypted = cryptographyHelper.Decrypt(certificate, encryptedToken);

            //Splitting it to dictionary
            Dictionary<string, string> dictionary = decrypted.ToDictionary();
            return new Token(dictionary["FacilityId"] ,dictionary["UserId"], dictionary["IP"]);
        }
    }
}