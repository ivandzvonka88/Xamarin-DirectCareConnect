
using System;
using System.Configuration;

namespace DCCHelper
{
    internal static class Base
    {
        public static string SMTPServer { get { return ConfigurationManager.AppSettings["SMTPServer"]; } }
        public static string SMTPServerPort { get { return ConfigurationManager.AppSettings["SMTPServerPort"]; } }
        public static string SMTPServerUser { get { return ConfigurationManager.AppSettings["SMTPServerUser"]; } }
        public static string SMTPServerPwd { get { return ConfigurationManager.AppSettings["SMTPServerPwd"]; } }
        public static string EmailAddress { get { return ConfigurationManager.AppSettings["EmailAddress"]; } }
        public static string TwilioAccount { get { return ConfigurationManager.AppSettings["TAcct"]; } }
        public static string TwilioFrom { get { return ConfigurationManager.AppSettings["TFrom"]; } }
        public static string TwilioToken { get { return ConfigurationManager.AppSettings["TCode"]; } }

        public static void Initialize()
        {
            if (string.IsNullOrEmpty(Base.SMTPServer) || string.IsNullOrEmpty(Base.SMTPServerPort) || string.IsNullOrEmpty(Base.EmailAddress) || string.IsNullOrEmpty(Base.SMTPServerPwd) || string.IsNullOrEmpty(Base.SMTPServerUser))
            {
                throw new Exception("Email settings are missing");
            }
            if (string.IsNullOrEmpty(Base.TwilioAccount) || string.IsNullOrEmpty(Base.TwilioFrom) || string.IsNullOrEmpty(Base.TwilioToken))
            {
                throw new Exception("SMS settings are missing");
            }
        }
    }
}