using System;
using System.Security;
using System.Text.RegularExpressions;

namespace Core.Encryption
{
    /* Password validation and integirty check class. */
    public static class PasswordValidator
    {
        /// <summary>
        /// Password complexity check: digit, upper case and lower case.
        /// </summary>
        /// <param name="password">Password string.</param>
        /// <returns>bool</returns>
        public static bool ValidatePassword(string password)
        {
            string patternPassword = @"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{12,500}$";
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
                        pwd = pwd.Remove(pwd.Length - 1);
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
    }
}
