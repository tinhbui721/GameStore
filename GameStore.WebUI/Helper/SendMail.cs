using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Net;

namespace GameStore.WebUI.Helper
{
    public class SendMail
    {
        public static bool SendEMail(string to, string subject, string body)
        {
            try
            {
                MailMessage mailMessage = new MailMessage(Email.emailSender, to, subject, body);
                mailMessage.IsBodyHtml = true;
                using (var smtpClient = new SmtpClient(Email.hostEmail, Email.portEmail))
                {
                    NetworkCredential credential = new NetworkCredential(Email.emailSender, Email.passwordSender);
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = credential;
                    //smtpClient.UseDefaultCredentials = false;
                    smtpClient.Send(mailMessage);

                }

            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }

    public static class Email
    {
        public static string hostEmail = "smtp.gmail.com";
        public static int portEmail = 587;
        public static string emailSender = "gamestore070201@gmail.com";
        public static string passwordSender = "zfhsvzfngubktigx";
    }
}