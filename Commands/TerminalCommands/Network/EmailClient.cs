using Core;
using System;
using System.Runtime.Versioning;
using PasswordValidator = Core.Encryption.PasswordValidator;
/*
 
Disabled
 
 */
namespace Commands.TerminalCommands.Network
{
    [SupportedOSPlatform("Windows")]
    public class EmailClient 
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
            // Define the email parameters.
            Console.WriteLine("** Enter your eMail Address **");
            s_mailFrom = Console.ReadLine();
            Console.WriteLine("** Enter your eMail Name (default blank) **");
            if (String.IsNullOrWhiteSpace(s_mailName = Console.ReadLine()))
            {
                s_mailName = "";
            }
            Console.WriteLine("** Enter your eMail Password **");
            s_mailPass = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
            Console.WriteLine();
            Console.WriteLine("** Enter destination eMail Address **");
            s_mailTo = Console.ReadLine();
            Console.WriteLine("** Enter eMail Title **");
            s_mailTitle = Console.ReadLine();
            Console.WriteLine("** Enter eMail Body Content **");
            s_mailBody = Console.ReadLine();
            //-----------------------------------


            // Sending the mail.
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
    }
}
