using System;
using System.Collections.Generic;
using System.Net.Mail;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;

namespace DCCHelper
{
    public static class CommunicationHelper
    {
        public static string SendEmail(string userEmail, string subject, string message, System.Web.HttpPostedFileBase file = null)
        {
            List<string> userEmails = new List<string>();
            userEmails.Add(userEmail);
            return SendEmail(userEmails, subject, message, file);
        }
        public static string SendEmail(List<string> userEmails, string subject, string message, System.Web.HttpPostedFileBase file = null)
        {
            SmtpClient mailer = new SmtpClient(Base.SMTPServer);
            mailer.Port = int.Parse(Base.SMTPServerPort);
            mailer.Credentials = new System.Net.NetworkCredential(Base.SMTPServerUser, Base.SMTPServerPwd);
            string msg = "";


            try
            {
                MailMessage mail = new MailMessage();
                Attachment attachment;

                if (file != null && !string.IsNullOrEmpty(file.FileName))
                {
                    attachment = new System.Net.Mail.Attachment(file.InputStream, file.FileName);
                    mail.Attachments.Add(attachment);
                }
                mail.From = new MailAddress(Base.EmailAddress);
                mail.Sender = new MailAddress(Base.EmailAddress);
                userEmails.ForEach(id => mail.To.Add(id));
                mail.Subject = subject;
                mail.Body = message;

                mailer.Send(mail);
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                throw ex;
            }
            finally
            {
                mailer.Dispose();
            }
            return msg;
        }
        public static string SendSMS(string phoneNumber, string message)
        {
            TwilioClient.Init(Base.TwilioAccount, Base.TwilioToken);
            var to = new PhoneNumber(string.Format("+1{0}", phoneNumber.Replace("-", "").Replace("(", "").Replace(")", "")));
            var sms = MessageResource.Create(to, body: message, from: new PhoneNumber(Base.TwilioFrom));
            if (sms.ErrorCode.HasValue)
            {
                return(string.Format("SMS Error #{0} ({1})", sms.ErrorCode, sms.ErrorMessage));
            }
            return string.Empty;
        }
    }
}
