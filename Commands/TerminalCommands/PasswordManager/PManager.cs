using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using PasswordValidator = Core.Encryption.PasswordValidator;

namespace Commands.TerminalCommands.PasswordManager
{
    /*Simple password manager to store localy sensitive authentification data from a specific application.
      Using Rijndael AES-256bit encryption for data and Argon2 for master password hash.
     */
    public class PManager : ITerminalCommand
    {
        public string Name => "pwm";
        private static int s_tries = 0;
        private static JavaScriptSerializer s_serializer;
        private static string s_helpMessage = @"A simple password manager to store locally the authentication data encrypted for a application using Rijndael AES-256 and Argon2 for password hash.
Usage of Password Manager commands:
  -h       : Display this message.
  -createv : Create a new vault.
  -delv    : Deletes an existing vault.
  -listv   : Displays the current vaults.
  -addapp  : Adds a new application to vault.
  -dela    : Deletes an existing application in a vault.
  -updatea : Updates account's password for an application in a vault.
  -lista   : Displays the existing applications in a vault.
";

        public void Execute(string arg)
        {

            try
            {
                if (arg.Length == 3)
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (arg == $"{Name} -h")
                    Console.WriteLine(s_helpMessage);
                if (arg.ContainsText("-createv"))
                    CreateVault();
                if (arg.ContainsText("-delv"))
                    DeleteVault();
                if (arg.ContainsText("-dela"))
                    DeleteAppUserData();
                if (arg.ContainsText("-listv"))
                    ListVaults();
                if (arg.ContainsText("-addapp"))
                    AddPasswords();
                if (arg.ContainsText("-lista"))
                    ReadPass();
                if (arg.ContainsText("-updatea"))
                    UpdateAppUserData();
            }
            catch (FileNotFoundException)
            {
                FileSystem.ErrorWriteLine("Vault was not found. Check command!");
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message + ". Check command!");
            }
        }

        /// <summary>
        /// Check maximum of tries. Used in while loops for exit them at a certain count.
        /// </summary>
        /// <returns></returns>
        private static bool CheckMaxTries()
        {
            s_tries++;
            if (s_tries >= 3)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "You have exceeded the number of tries!");
                s_tries = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a new vault.
        /// </summary>
        private static void CreateVault()
        {
            string vaultName;
            string masterPassword1;
            string masterPassword2;
            bool userNameIsValid = false;
            bool passValidation = false;
            do
            {
                Console.WriteLine("Vault Name: ");
                vaultName = Console.ReadLine();
                vaultName = vaultName.ToLower();
                var vaultFiles = Directory.GetFiles(GlobalVariables.passwordManagerDirectory);
                if (vaultName.Length < 3)
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault name must be at least 3 characters long!");
                }
                else if (string.Join("\n", vaultFiles).Contains($"{vaultName}.x"))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} already exist!");
                }
                else
                {
                    userNameIsValid = true;
                }
                if (CheckMaxTries())
                    return;
            } while (userNameIsValid == false);
            s_tries = 0;
            do
            {
                Console.WriteLine("Master Password: ");
                masterPassword1 = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
                Console.WriteLine();
                Console.WriteLine("Confirm Master Password: ");
                masterPassword2 = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
                Console.WriteLine();
                if (masterPassword1 != masterPassword2)
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Passwords are not the same!");
                }
                else
                {
                    passValidation = true;
                }
                if (!PasswordValidator.ValidatePassword(masterPassword2))
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Password must be at least 12 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");
                if (CheckMaxTries())
                    return;
            } while ((masterPassword1 != masterPassword2) || !PasswordValidator.ValidatePassword(masterPassword2));
            s_tries = 0;
            if (passValidation)
            {
                string sealVault = Core.Encryption.AES.Encrypt(string.Empty, masterPassword1);
                if (!sealVault.Contains("Error encrypting"))
                {
                    File.WriteAllText(GlobalVariables.passwordManagerDirectory + $"\\{vaultName}.x", sealVault);
                    WordColorInLine("\n[+] Vault ", vaultName, " was created!\n", ConsoleColor.Cyan);
                    return;
                }
                FileSystem.ErrorWriteLine(sealVault + ". Check command!");
            }
        }

        /// <summary>
        /// Deletes an existing vault.
        /// </summary>
        private static void DeleteVault()
        {
            Console.WriteLine("Enter vault name: ");
            string vaultName = Console.ReadLine();
            if (string.IsNullOrEmpty(vaultName))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"You must type the vault name!");
                return;
            }
            vaultName = vaultName.ToLower();
            var vaultFiles = Directory.GetFiles(GlobalVariables.passwordManagerDirectory);
            while (!string.Join("\n", vaultFiles).Contains($"{vaultName}.x"))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} does not exist!");
                Console.WriteLine("Enter vault name: ");
                vaultName = Console.ReadLine();
            }
            s_tries = 0;
            if (string.Join("\n", vaultFiles).Contains(vaultName))
            {
                if (DeleteConfirmationCheck(vaultName))
                {
                    File.Delete(GlobalVariables.passwordManagerDirectory + $"\\{vaultName}.x");
                    WordColorInLine("\n[-] Vault ", vaultName, " was deleted!\n", ConsoleColor.Cyan);
                    return;
                }
                return;
            }
            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} does not exist anymore!");
        }

        /// <summary>
        /// Confirmation message for vault delete.
        /// </summary>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        private static bool DeleteConfirmationCheck(string vaultName)
        {
            WordColorInLine("\n Do you want to delete ", vaultName, " vault? Press Y [yes] / N [no]:\n", ConsoleColor.Cyan);
            string response = Console.ReadLine().ToLower();
            if (response == "y")
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// List current vaults.
        /// </summary>
        private static void ListVaults()
        {
            string outFiles = string.Empty;
            var getFiles = Directory.GetFiles(GlobalVariables.passwordManagerDirectory);
            int filesCount = getFiles.Count();
            foreach (var file in getFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                string vaultName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 2);
                outFiles += "----------------\n";
                outFiles += vaultName + Environment.NewLine;
            }
            if (filesCount == 0)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "There are no vaults created!");
                return;
            }
            Console.WriteLine("List of current vaults:");
            Console.WriteLine(outFiles + "----------------");
        }


        /// <summary>
        /// Check if vault exists.
        /// </summary>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        private static bool CheckVaultExist(string vaultName)
        {
            var getFiles = Directory.GetFiles(GlobalVariables.passwordManagerDirectory);
            foreach (var file in getFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name.Contains(vaultName))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Add new application to a current vault.
        /// </summary>
        private static void AddPasswords()
        {
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
            Console.WriteLine();
            string decryptVault = Core.Encryption.AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            Console.WriteLine("Enter application name:");
            string application = Console.ReadLine();
            while (application.Length < 3)
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "The length of application name should be at least 3 characters!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
            string account = Console.ReadLine();
            while (account.Length < 3)
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "The length of account name should be at least 3 characters!");
                WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
                account = Console.ReadLine();
            }
            s_tries = 0;
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    s_serializer = new JavaScriptSerializer();
                    var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                    if (line.Length > 0)
                    {
                        if (outJson["site/application"] == application && outJson["account"] == account)
                        {
                            FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault already contains {account} account in {application} application!");
                            return;
                        }
                    }
                }
            }

            WordColorInLine("Enter password for ", account, ":", ConsoleColor.Green);
            string password = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
            Console.WriteLine();
            var keyValues = new Dictionary<string, object>
                {
                    { "site/application", application },
                    { "account", account },
                    { "password", password },
                };
            s_serializer = new JavaScriptSerializer();
            string encryptdata = Core.Encryption.AES.Encrypt(decryptVault + "\n" + s_serializer.Serialize(keyValues), masterPassword);
            if (File.Exists(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x"))
            {
                File.WriteAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x", encryptdata);
                WordColorInLine("\n[+] Data for ", application, " is encrypted and added to vault!\n", ConsoleColor.Magenta);
                return;
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
            }
        }

        /// <summary>
        /// Displays applicaitons from an existing vault.
        /// </summary>
        private static void ReadPass()
        {
            string decryptVault = DecryptData();
            if (decryptVault.Contains("Error decrypting"))
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            if (string.IsNullOrEmpty(decryptVault))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "There is no data saved in this vault!");
                return;
            }
            Console.WriteLine("Enter application name (leave blank for all applications):");
            string application = Console.ReadLine();
            if (application.Length > 0)
            {
                WordColorInLine("This is your decrypted data for ", application, ":", ConsoleColor.Magenta);
            }
            else
            {
                Console.WriteLine("This is your decrypted data for the entire vault:");
            }
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(application) && line.Length > 0)
                    {
                        s_serializer = new JavaScriptSerializer();
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"].Contains(application))
                        {
                            Console.WriteLine("-------------------------");
                            Console.WriteLine($"Application Name: ".PadRight(20, ' ') + outJson["site/application"]);
                            Console.WriteLine($"Account Name: ".PadRight(20, ' ') + outJson["account"]);
                            Console.WriteLine($"Password: ".PadRight(20, ' ') + outJson["password"]);
                        }
                    }
                }
                Console.WriteLine("-------------------------");
            }
        }

        /// <summary>
        /// Decrypts vault data.
        /// </summary>
        /// <returns></returns>
        private static string DecryptData()
        {
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return string.Empty;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
            Console.WriteLine();
            string decryptVault = Core.Encryption.AES.Decrypt(encryptedData, masterPassword);
            return decryptVault;
        }

        /// <summary>
        /// Delete an application from vault.
        /// </summary>
        private static void DeleteAppUserData()
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
            Console.WriteLine();
            string decryptVault = Core.Encryption.AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            Console.WriteLine("Enter application name:");
            string application = Console.ReadLine();
            while (string.IsNullOrEmpty(application))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Application name should not be empty!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            while (!decryptVault.Contains(application))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Application {application} does not exist!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
            string accountName = Console.ReadLine();
            while (string.IsNullOrEmpty(accountName))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Account name should not be empty!");
                WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
                accountName = Console.ReadLine();
            }
            s_tries = 0;
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0)
                    {
                        listApps.Add(line);
                        s_serializer = new JavaScriptSerializer();
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            listApps.Remove(line);
                            accountCheck = true;
                        }
                    }
                }
                string encryptdata = Core.Encryption.AES.Encrypt(string.Join("\n", listApps), masterPassword);
                listApps.Clear();
                if (File.Exists(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x"))
                {
                    if (accountCheck)
                    {
                        File.WriteAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x", encryptdata);
                        Console.Write("\n[-]Account ");
                        FileSystem.ColorConsoleText(ConsoleColor.Green, accountName);
                        Console.Write(" for ");
                        FileSystem.ColorConsoleText(ConsoleColor.Magenta, application);
                        Console.Write(" was deleted!" + Environment.NewLine);
                        return;
                    }
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Account {accountName} does not exist!");
                    return;
                }
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
            }
        }


        /// <summary>
        /// Update account's password from an application.
        /// </summary>
        private static void UpdateAppUserData()
        {
            List<string> listApps = new List<string>();
            bool accountCheck = false;
            Console.WriteLine("Enter vault name:");
            string vault = Console.ReadLine();
            vault = vault.ToLower();
            while (!CheckVaultExist(vault))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            s_tries = 0;
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            WordColorInLine("Enter master password for ", vault, " vault:", ConsoleColor.Cyan);
            string masterPassword = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
            Console.WriteLine();
            string decryptVault = Core.Encryption.AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            Console.WriteLine("Enter application name:");
            string application = Console.ReadLine();
            while (string.IsNullOrEmpty(application))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Application name should not be empty!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            while (!decryptVault.Contains(application))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Application {application} does not exist!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
            string accountName = Console.ReadLine();
            while (string.IsNullOrEmpty(accountName))
            {
                if (CheckMaxTries())
                    return;
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Account name should not be empty!");
                WordColorInLine("Enter account name for ", application, ":", ConsoleColor.Magenta);
                accountName = Console.ReadLine();
            }
            s_tries = 0;
            WordColorInLine("Enter new password for ", accountName, ":", ConsoleColor.Green);
            string password = PasswordValidator.ConvertSecureStringToString(PasswordValidator.GetHiddenConsoleInput());
            Console.WriteLine();
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    s_serializer = new JavaScriptSerializer();
                    if (line.Length > 0)
                    {
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"] == application && outJson["account"] == accountName)
                        {
                            var keyValues = new Dictionary<string, object>
                            {
                                 { "site/application", application },
                                 { "account", accountName },
                                 { "password", password },
                            };
                            accountCheck = true;
                            listApps.Add(s_serializer.Serialize(keyValues));
                        }
                        else
                        {
                            listApps.Add(line);
                        }
                    }
                }
                string encryptdata = Core.Encryption.AES.Encrypt(string.Join("\n", listApps), masterPassword);
                listApps.Clear();
                if (File.Exists(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x"))
                {
                    if (accountCheck)
                    {
                        File.WriteAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x", encryptdata);
                        WordColorInLine("\n[*]Password for ", accountName, " was updated!\n", ConsoleColor.Green);
                        return;
                    }
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Account {accountName} does not exist!");
                    return;
                }
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
            }
        }

        /// <summary>
        /// Color a word from a string.
        /// </summary>
        /// <param name="beforeText">Text to be added befor word.</param>
        /// <param name="word">Word to be colored.</param>
        /// <param name="afterText">Text after the colored word.</param>
        /// <param name="color">Console color for the metioned word.</param>
        private static void WordColorInLine(string beforeText, string word, string afterText, ConsoleColor color)
        {
            Console.Write(beforeText);
            FileSystem.ColorConsoleText(color, word);
            Console.Write(afterText + Environment.NewLine);
        }
    }
}
