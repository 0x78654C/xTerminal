using System;
using Core;
using System.Text;

namespace Commands.TerminalCommands.Network
{
    public class EmailClient : ITerminalCommand
    {
        private static string s_mailFrom = string.Empty;
        private static string s_mailPass = string.Empty;
        private static string s_mailName = string.Empty;
        private static string s_mailTo = string.Empty;
        private static string s_mailTitle = string.Empty;
        private static string s_mailBody = string.Empty;
        public string Name => "email";
        public void Execute(string arg)
        {
            Console.WriteLine("*******************************************");
            Console.WriteLine("**********Email Sender Client**************");
            Console.WriteLine("*******************************************");
            Console.WriteLine(" ");
            //difine the email parameters
            Console.WriteLine("** Enter your eMail address **");
            s_mailFrom = Console.ReadLine();
            Console.WriteLine("** Enter your eMail Name (default blank) **");
            if (String.IsNullOrWhiteSpace(s_mailName = Console.ReadLine()))
            {
                s_mailName = "";
            }
            Console.WriteLine("** Enter your eMail password **");
            s_mailPass = GetHiddenConsoleInput();
            Console.WriteLine("** Enter destination eMail address **");
            s_mailTo = Console.ReadLine();
            Console.WriteLine("** Enter eMail title **");
            s_mailTitle = Console.ReadLine();
            Console.WriteLine("** Enter eMail body content **");
            s_mailBody = Console.ReadLine();
            //-----------------------------------


            //sending the mail
            try
            {
                eMailS.MailSend(s_mailFrom, s_mailName, s_mailPass, s_mailTo, s_mailTitle, s_mailBody);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
            }
            //----------------------
        }

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
    }
}
