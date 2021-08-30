using System;
using System.Net.Mail;
using System.Text;

namespace Core
{
    //Class for sending emails (gmail, yahoo, microsoft all)

    public class eMailS
    {
        /// <summary>
        /// MailSend function. Works only with gmail, microsoft(all), and yahoo!
        /// </summary>
        /// <param name="senderEmail">Senders email address.</param>
        /// <param name="senderName">Senders name.</param>
        /// <param name="senderPassword">Senders password.</param>
        /// <param name="receiverEmail">Receivers email address.</param>
        /// <param name="emailSubject">Mail Subject.</param>
        /// <param name="emailBody">Mail Body.</param>
        public static void MailSend(string senderEmail, string senderName, string senderPassword, string receiverEmail, string emailSubject, string emailBody)
        {
            try
            {
                string _date = DateTime.Now.ToString("yyyy MM dd HH:MM:ss");
                if (NetWork.inetCK())
                {
                    int port = 587;
                    string _smtp = SmtpCheck(senderEmail);
                    if(_smtp== "No valid SMTP Server")
                    {
                        Console.WriteLine($"[{_date}] No valid SMTP Server. Accepted email clients are: Microsoft(live, hotmail, outlook), Yahoo and Gmail!");
                        return;
                    }
                 
                    MailMessage msg = new MailMessage(); //create the message
                    msg.To.Add(receiverEmail);
                    msg.From = new MailAddress(senderEmail, senderName, Encoding.UTF8);
                    msg.Subject = emailSubject;
                    msg.SubjectEncoding = Encoding.UTF8;
                    msg.Body = emailBody;
                    msg.BodyEncoding = Encoding.UTF8;
                    msg.IsBodyHtml = false;
                    msg.Priority = MailPriority.Normal;
                    SmtpClient client = new SmtpClient();
                    client.Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword);
                    client.Port = port;
                    client.Host = _smtp;
                    client.EnableSsl = true;

                    try
                    {
                        client.Send(msg);
                        Console.WriteLine($"[{_date}] Email sent to " + receiverEmail);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.ToString());
                    }
                }
                else
                {
                    Console.WriteLine($"[{_date}] No internet connection!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
        }

        // We check for proper email client. 
        // Accepted are Microsoft, Yahoo, Gmail.
        private static string SmtpCheck(string senderEmail)
        {
            string smtpServer;

            if (senderEmail.Contains("@gmail."))
            {
                smtpServer = "smtp.gmail.com";
            }
            else if (senderEmail.Contains("@yahoo."))
            {
                smtpServer = "smtp.mail.yahoo.com";
            }
            else if (senderEmail.Contains("@live.") || senderEmail.Contains("@hotmail.") || senderEmail.Contains("@outlook."))
            {
                smtpServer = "smtp.live.com";
            }
            else
            {
                smtpServer = "No valid SMTP Server";
            }
            return smtpServer;
        }
    }
}
