using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DCC.ModelsLegacy;
using System.Net.Mail;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
namespace DCC.Helpers
{
    //public class EmailHelper
    //{

    //    public string communication(string userEmail, string subject, string message, string phone, bool isMail, System.Web.HttpPostedFileBase file = null)
    //    {
    //        SmtpClient mailer = new SmtpClient(ServiceUtilities.SMTPServer);
    //        mailer.Port = int.Parse(ServiceUtilities.SMTPServerPort);
    //        mailer.Credentials =
    //        new System.Net.NetworkCredential(ServiceUtilities.SMTPServerUser, ServiceUtilities.SMTPServerPwd);
    //        string msg = "";

    //        //SmtpClient mailer = new SmtpClient(ServiceUtilities.SMTPServer);
    //        try
    //        {
    //            if (isMail)
    //            {
    //                MailMessage mail = new MailMessage();
    //                Attachment attachment;
                   
    //                if (file != null && !string.IsNullOrEmpty(file.FileName))
    //                {
    //                    attachment = new System.Net.Mail.Attachment(file.InputStream, file.FileName);
    //                    mail.Attachments.Add(attachment);
    //                }
    //                mail.From = new MailAddress(ServiceUtilities.EmailAddress);
    //                mail.Sender = new MailAddress(ServiceUtilities.EmailAddress);
    //                mail.To.Add(userEmail);
    //                mail.Subject = subject;
    //                mail.Body = message;

    //                //mailer.Send(ServiceUtilities.EmailAddress, userEmail, subject, message);
    //                mailer.Send(mail);

    //            }
    //            else
    //            {
    //                TwilioClient.Init(ServiceUtilities.TwilioAccount, ServiceUtilities.TwilioToken);
    //                var to = new PhoneNumber(string.Format("+1{0}", phone.Replace("-", "").Replace("(", "").Replace(")", "")));
    //                var sms = MessageResource.Create(to, body: message, from: new PhoneNumber(ServiceUtilities.TwilioFrom));
    //                if (sms.ErrorCode.HasValue)
    //                {
    //                    throw new Exception(string.Format("SMS Error #{0} ({1})", sms.ErrorCode, sms.ErrorMessage));
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            msg = ex.Message;
    //            //throw ex;
    //        }
    //        finally
    //        {
    //            mailer.Dispose();
    //        }
    //        return msg;
    //    }

    //    public static string SendEmail(string userEmail, string subject, string message, System.Web.HttpPostedFileBase file = null)
    //    {
    //        List<string> userEmails = new List<string>();
    //        userEmails.Add(userEmail);
    //        return SendEmail(userEmails, subject, message, file);
    //    }
    //    public static string SendEmail(List<string> userEmails, string subject, string message, System.Web.HttpPostedFileBase file = null)
    //    {
    //        SmtpClient mailer = new SmtpClient(ServiceUtilities.SMTPServer);
    //        mailer.Port = int.Parse(ServiceUtilities.SMTPServerPort);
    //        mailer.Credentials = new System.Net.NetworkCredential(ServiceUtilities.SMTPServerUser, ServiceUtilities.SMTPServerPwd);
    //        string msg = "";

    //        try
    //        {
    //            MailMessage mail = new MailMessage();
    //            Attachment attachment;

    //            if (file != null && !string.IsNullOrEmpty(file.FileName))
    //            {
    //                attachment = new System.Net.Mail.Attachment(file.InputStream, file.FileName);
    //                mail.Attachments.Add(attachment);
    //            }
    //            mail.From = new MailAddress(ServiceUtilities.EmailAddress);
    //            mail.Sender = new MailAddress(ServiceUtilities.EmailAddress);
    //            userEmails.ForEach(id => mail.To.Add(id));
    //            mail.Subject = subject;
    //            mail.Body = message;

    //            mailer.Send(mail);
    //        }
    //        catch (Exception ex)
    //        {
    //            msg = ex.Message;
    //            throw ex;
    //        }
    //        finally
    //        {
    //            mailer.Dispose();
    //        }
    //        return msg;
    //    }
    //}
}