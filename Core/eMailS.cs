using System;
using System.Net.Mail;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
                if (NetWork.IntertCheck())
                {
                    int port = 587;
                    string _smtp = SmtpCheck(senderEmail);
                    if (_smtp == "No valid SMTP Server")
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
                        FileSystem.ErrorWriteLine(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"[{_date}] No internet connection!");
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
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

        /// <summary>
        /// Hidding password imput for strings
        /// </summary>
        /// <returns></returns>
        public static string GetHiddenConsoleInput()
        {
            string pwd = string.Empty;
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.Remove(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (i.KeyChar != '\u0000')
                {
                    pwd += (i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd;
        }

        /// <summary>
        /// Password complexity check: digit, upper case and lower case.
        /// </summary>
        /// <param name="password">Password string.</param>
        /// <returns>bool</returns>
        public static bool ValidatePassword(string password)
        {
            string patternPassword = @"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{10,500}$";
            if (!string.IsNullOrEmpty(password))
            {
                if (CheckSpaceChar(password))
                {
                    return false;
                }

                if (!Regex.IsMatch(password, patternPassword))
                {
                    return false;
                }

                if (!SpecialCharCheck(password))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check string for special character.
        /// </summary>
        /// <param name="input">Password string.</param>
        /// <returns></returns>
        private static bool SpecialCharCheck(string input)
        {
            string specialChar = @"\|!#$%&/()=?»«@£§€{}.-;'<>_,";
            if (input.IndexOfAny(specialChar.ToCharArray()) > -1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check for empty space in password.
        /// </summary>
        /// <param name="input">Password string.</param>
        /// <returns></returns>
        private static bool CheckSpaceChar(string input)
        {
            if (input.Contains(" ")) { return true; }
            return false;
        }
    }
}
