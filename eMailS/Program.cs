using System;
using System.Text;
using Core;

namespace eMail
{
    /*
     Short email client sender
     */
    class Program
    {
        private static string mailFrom = string.Empty;
        private static string mailPass = string.Empty;
        private static string mailName = string.Empty;
        private static string mailTo = string.Empty;
        private static string mailTitle = string.Empty;
        private static string mailBody = string.Empty;
        /// <summary>
        /// Hidding password imput for strings
        /// </summary>
        /// <returns></returns>
        private static string GetHiddenConsoleInput()
        {
            StringBuilder input = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace && input.Length > 0) input.Remove(input.Length - 1, 1);
                else if (key.Key != ConsoleKey.Backspace) input.Append(key.KeyChar);
            }
            return input.ToString();
        }
        static void Main(string[] args)
        {
            Console.WriteLine("*******************************************");
            Console.WriteLine("**********Email Sender Client**************");
            Console.WriteLine("*******************************************");
            Console.WriteLine(" ");
            //difine the email parameters
            Console.WriteLine("** Enter your eMail address **");
            mailFrom = Console.ReadLine();
            Console.WriteLine("** Enter your eMail Name (default blank) **");
            if (String.IsNullOrWhiteSpace(mailName = Console.ReadLine()))
            {
                mailName = "";
            }
            Console.WriteLine("** Enter your eMail password **");
            mailPass = GetHiddenConsoleInput();
            Console.WriteLine("** Enter destination eMail address **");
            mailTo = Console.ReadLine();
            Console.WriteLine("** Enter eMail title **");
            mailTitle = Console.ReadLine();
            Console.WriteLine("** Enter eMail body content **");
            mailBody = Console.ReadLine();
            //-----------------------------------


            //sending the mail
            try
            {
                Core.eMailS.MailSend(mailFrom, mailName, mailPass, mailTo, mailTitle, mailBody);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
            }
            //----------------------
        }
    }
}
