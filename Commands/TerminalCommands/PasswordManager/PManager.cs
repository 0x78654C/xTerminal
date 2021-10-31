using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Core;
using System.Web.Script.Serialization;

namespace Commands.TerminalCommands.PasswordManager
{
    public class PManager : ITerminalCommand
    {
        public string Name => "pwm";
        private static JavaScriptSerializer s_serializer;
        private static string s_helpMessage = @"Usage of Password Manager commands:
  -h       : Display this message.
  -createv : Create a new vault.
  -delv    : Deletes an existing vault.
  -listv   : Displays the current vaults.
  -addapp  : Adds a new application to vault.
  -dela    : Deletes an existing application in a vault.
  -updatea : Updates account's password for an application in a vault.
  -lista   : Displays the existing applicaitons in a vault.
";

        public void Execute(string arg)
        {
            try
            {
                if (arg.ContainsText("-h"))
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
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message + " Check command!");
            }
        }


        /// <summary>
        /// Creates a new vault.
        /// </summary>
        private void CreateVault()
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

            } while (userNameIsValid == false);

            do
            {
                Console.WriteLine("Master Password: ");
                masterPassword1 = eMailS.GetHiddenConsoleInput();
                Console.WriteLine();
                Console.WriteLine("Confirm Master Password: ");
                masterPassword2 = eMailS.GetHiddenConsoleInput();
                Console.WriteLine();
                if (masterPassword1 != masterPassword2)
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Passwords are not the same!");
                }
                else
                {
                    passValidation = true;
                }
                if (!eMailS.ValidatePassword(masterPassword2))
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Password must be at least 10 characters, and must include at least one upper case letter, one lower case letter, one numeric digit, one special character and no space!");

            } while ((masterPassword1 != masterPassword2) && !eMailS.ValidatePassword(masterPassword2));

            if (passValidation)
            {
                File.WriteAllText(GlobalVariables.passwordManagerDirectory + $"\\{vaultName}.x", Core.Encryption.AES.Encrypt(string.Empty, masterPassword1));
                Console.Write($"Vault ");
                FileSystem.ColorConsoleText(ConsoleColor.Cyan, vaultName);
                Console.Write($" was created!\n");
            }
        }

        /// <summary>
        /// Deletes an existing vault.
        /// </summary>
        private static void DeleteVault()
        {
            Console.WriteLine("Enter vault name: ");
            string vaultName = Console.ReadLine();
            vaultName = vaultName.ToLower();
            var vaultFiles = Directory.GetFiles(GlobalVariables.passwordManagerDirectory);
            while (!string.Join("\n", vaultFiles).Contains($"{vaultName}.x"))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} does not exist!");
                Console.WriteLine("Enter vault name: ");
                vaultName = Console.ReadLine();
            }
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vaultName}.x");
            Console.WriteLine("Master Password: ");
            string masterPassword = eMailS.GetHiddenConsoleInput();
            Console.WriteLine();
            string decryptVault = Core.Encryption.AES.Decrypt(encryptedData, masterPassword);

            if (decryptVault.Contains("Error decrypting"))
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }

            if (string.Join("\n", vaultFiles).Contains(vaultName))
            {
                File.Delete(GlobalVariables.passwordManagerDirectory + $"\\{vaultName}.x");
                Console.WriteLine($"Vault {vaultName} was deleted!");
            }
            else
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vaultName} does not exist anymore!");
            }
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
                outFiles += "----------------\n";
                outFiles += fileInfo.Name + Environment.NewLine;
            }
            if (filesCount == 0)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "There are no vaults created!");
                return;
            }
            Console.WriteLine("List of current vaults:\n");
            Console.WriteLine(outFiles+ "----------------");
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
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            Console.WriteLine("Enter vault master password:");
            string masterPassword = eMailS.GetHiddenConsoleInput();
            Console.WriteLine();
            string decryptVault = Core.Encryption.AES.Decrypt(encryptedData, masterPassword);
            if (decryptVault.Contains("Error decrypting"))
            {
                FileSystem.ErrorWriteLine("Something went wrong. Check master password or vault name!");
                return;
            }
            Console.WriteLine("Enter application name:");
            string site = Console.ReadLine();
            while (site.Length < 3)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "The length of application name should be at least 3 characters!");
                Console.WriteLine($"Enter account name for {site}:");
                site = Console.ReadLine();
            }
            Console.WriteLine("Enter account name for application:");
            string account = Console.ReadLine();
            while (account.Length < 3)
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "The length of account name should be at least 3 characters!");
                Console.WriteLine($"Enter account name for {site}:");
                account = Console.ReadLine();
            }
            Console.WriteLine($"Enter password for {account}:");
            string password = eMailS.GetHiddenConsoleInput();
            Console.WriteLine();
            var keyValues = new Dictionary<string, object>
                {
                    { "site/application", site },
                    { "account", account },
                    { "password", password },
                };

            s_serializer = new JavaScriptSerializer();
            string encryptdata = Core.Encryption.AES.Encrypt(decryptVault + "\n" + s_serializer.Serialize(keyValues), masterPassword);
            if (File.Exists(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x"))
            {
                File.WriteAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x", encryptdata);
                Console.WriteLine($"Data for {site} is encrypted and added to vault!");
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
            Console.WriteLine("Enter application name (leave blank for all applications):");
            string site = Console.ReadLine();
            Console.WriteLine($"This is your decrypted data for {site} application");
            using (var reader = new StringReader(decryptVault))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(site) && line.Length > 0)
                    {
                        s_serializer = new JavaScriptSerializer();
                        var outJson = s_serializer.Deserialize<Dictionary<string, string>>(line);
                        if (outJson["site/application"].Contains(site))
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
                Console.WriteLine("Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            Console.WriteLine("Enter vault master password:");
            string masterPassword = eMailS.GetHiddenConsoleInput();
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
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            Console.WriteLine("Enter vault master password:");
            string masterPassword = eMailS.GetHiddenConsoleInput();
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
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Application name should not be empty!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            while (!decryptVault.Contains(application))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Application {application} does not exist!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            Console.WriteLine("Enter account name:");
            string accountName = Console.ReadLine();
            while (string.IsNullOrEmpty(accountName))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Account name should not be empty!");
                Console.WriteLine("Enter account name:");
                accountName = Console.ReadLine();
            }
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
                        Console.WriteLine($"Account {accountName} for {application} was deleted!");
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
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Vault does not exist!");
                Console.WriteLine("Enter vault name:");
                vault = Console.ReadLine();
            }
            string encryptedData = File.ReadAllText(GlobalVariables.passwordManagerDirectory + $"\\{vault}.x");
            Console.WriteLine("Enter vault master password:");
            string masterPassword = eMailS.GetHiddenConsoleInput();
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
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Application name should not be empty!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            while (!decryptVault.Contains(application))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Application {application} does not exist!");
                Console.WriteLine("Enter application name:");
                application = Console.ReadLine();
            }
            Console.WriteLine("Enter account name:");
            string accountName = Console.ReadLine();
            while (string.IsNullOrEmpty(accountName))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "Account name should not be empty!");
                Console.WriteLine("Enter account name:");
                accountName = Console.ReadLine();
            }

            Console.WriteLine("Enter new password:");
            string password = eMailS.GetHiddenConsoleInput();
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
                        Console.WriteLine($"Password for {accountName} was updated!");
                        return;
                    }
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Account {accountName} does not exist!");
                    return;
                }
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Vault {vault} does not exist!");
            }
        }
    }
}
