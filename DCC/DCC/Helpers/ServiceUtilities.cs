using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using DCC.ModelsLegacy;

namespace DCC.Helpers
{
    public static class ServiceUtilities
    {
        public static string SMTPServerPwd { get { return ConfigurationManager.AppSettings["SMTPServerPwd"]; } }
        public static string SMTPServer { get { return ConfigurationManager.AppSettings["SMTPServer"]; } }
        public static string SMTPServerPort { get { return ConfigurationManager.AppSettings["SMTPServerPort"]; } }
        public static string SMTPServerUser { get { return ConfigurationManager.AppSettings["SMTPServerUser"]; } }
        public static int CredentialExpirationMonths { get { return Convert.ToInt32(ConfigurationManager.AppSettings["CredentialExpirationMonths"]); } }

        public static string EKey { get { return ConfigurationManager.AppSettings["EncryptKey"]; } }
        public static string EmailAddress
        {
            get
            {
                return ConfigurationManager.AppSettings["EmailAddress"];
            }
        }
        public static string SupportEmail
        {
            get
            {
                return ConfigurationManager.AppSettings["SupportEmail"];
            }
        }
        public static string TwilioAccount
        {
            get
            {
                return ConfigurationManager.AppSettings["TAcct"];

            }
        }
        public static string TwilioFrom
        {
            get
            {
                return ConfigurationManager.AppSettings["TFrom"];

            }
        }
        public static string TwilioToken
        {
            get
            {
                return ConfigurationManager.AppSettings["TCode"];

            }
        }
        public static int SessionTimeoutMinutes
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["SessionTimeoutMinutes"]);
            }
        }

        public static string StaffWebSiteDomain
        {
            get
            {
                return ConfigurationManager.AppSettings["StaffWebSiteDomain"];
            }
        }

        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["TC"].ConnectionString;

            }
        }
        public static string SFTPKeyContainer { get { return ConfigurationManager.AppSettings["SFTPKeyContainer"]; } }
        public static string SFTPSupportEmail { get { return ConfigurationManager.AppSettings["SFTPSupportEmail"]; } }
        public static string FTPHost { get { return ConfigurationManager.AppSettings["FTPHost"]; } }
    }
}