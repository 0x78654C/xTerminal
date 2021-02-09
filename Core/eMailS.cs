using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace Core
{
    //Library for sending emails (gmail, yahoo, microsoft all)

    public class eMailS
    {
        /// <summary>
        /// MailSend function. Works only with gmail, microsoft(all), and yahoo!
        /// </summary>
        /// <param name="mailFrom"></param>
        /// <param name="pass"></param>
        /// <param name="mailTo"></param>
        /// <param name="titile"></param>
        /// <param name="body"></param>
        public static void MailSend(string mailFrom,string fromName, string pass, string mailTo, string titile, string body)
        {
            try
            {
                string _date = DateTime.Now.ToString("yyyy MM dd HH:MM:ss");
                if (NetWork.inetCK())
                {


                    string _smtp = "";
                    int port = 587;
                    if (mailFrom.Contains("@gmail."))
                    {
                        _smtp = "smtp.gmail.com";               
                    }
                    if (mailFrom.Contains("@yahoo."))
                    {
                        _smtp = "smtp.mail.yahoo.com";

                    }
                    if (mailFrom.Contains("@live.") || mailFrom.Contains("@hotmail.") || mailFrom.Contains("@outlook."))
                    {
                        _smtp = "smtp.live.com";

                    }

                    //---------------------------
                    System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(); //create the message
                    msg.To.Add(mailTo);
                    msg.From = new MailAddress(mailFrom, fromName,Encoding.UTF8);//can be modified with name
                    msg.Subject = titile;//MachineName.ToString();
                    msg.SubjectEncoding = System.Text.Encoding.UTF8;
                    msg.Body = body;
                    msg.BodyEncoding = System.Text.Encoding.UTF8;
                    msg.IsBodyHtml = false;
                    msg.Priority = MailPriority.Normal;
                    SmtpClient client = new SmtpClient();
                    client.Credentials = new System.Net.NetworkCredential(mailFrom, pass);
                    client.Port = port;

                    client.Host = _smtp;
                    client.EnableSsl = true;

                    try
                    {
                        client.Send(msg);
                        Console.WriteLine("[" + _date + "]" + "Email sent to " + mailTo);

                    }
                    catch (Exception e)
                    {

                        Console.WriteLine("Error: " + e.ToString());

                    }
                }
                else
                {
                    Console.WriteLine("[" + _date + "]" + "No internet connection!");
                }
            }catch(Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

        }
    }
}
