using Core.Encryption;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ProcessStart
    {
        /// <summary>
        /// Process execution with arguments.
        /// </summary>
        /// <param name="input">File name.</param>
        /// <param name="arguments">Specific file arguments.</param>
        /// <param name="fileCheck">Check file if exists before process exection. </param>
        /// <param name="asAdmin">Run as different user.</param>
        private static string s_currentDirectory;

        /// <summary>
        /// Get path of assembly.
        /// </summary>
        /// <param name="executableFilePath"></param>
        /// <returns></returns>
        private static string GetExecutablePath(string executableFilePath) => Path.GetDirectoryName(executableFilePath);

        /// <summary>
        /// Execute process command.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="arguments"></param>
        /// <param name="fileCheck"></param>
        /// <param name="asAdmin"></param>
        /// <param name="waitForExit"></param>
        public static void ProcessExecute(string input, string arguments, bool fileCheck, bool asAdmin, bool waitForExit)
        {
            try
            {
                var process = new Process();

                bool exe = !input.Trim().EndsWith(".exe") && !input.Trim().EndsWith(".msi");


                // Check is execautable or not.
                if (exe)
                {
                    arguments = $@"/c start {input} {arguments}";
                    input = null;
                }
                process.StartInfo = new ProcessStartInfo(input);

                if (asAdmin)
                {

                    var secureString = new System.Security.SecureString();
                    if (exe)
                        process.StartInfo.FileName = "cmd";
                    process.StartInfo.WorkingDirectory = GetExecutablePath(input);
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Arguments = arguments.Trim();
                    Console.Write("User name: ");
                    var userName = Console.ReadLine();
                    if (string.IsNullOrEmpty(userName))
                    {
                        Console.WriteLine();
                        FileSystem.ErrorWriteLine("User name must be provieded!");
                        return;
                    }
                    process.StartInfo.UserName = userName;
                    Console.Write("Passwod: ");
                    var password = PasswordValidator.GetHiddenConsoleInput();
                    if (password.Length <= 0)
                    {
                        FileSystem.ErrorWriteLine($"Password for {userName} must be provided!");
                        return;
                    }
                    process.StartInfo.Password = password;
                    Console.WriteLine();
                    Console.Write("Domain(optional): ");
                    var domain = Console.ReadLine() ?? string.Empty;
                    if (!string.IsNullOrEmpty(domain))
                        process.StartInfo.Domain = domain;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardError = true;
                }
                else
                {
                    if (exe)
                        process.StartInfo.FileName = "cmd";
                    process.StartInfo.WorkingDirectory = GetExecutablePath(input);
                    process.StartInfo.UseShellExecute = false;
                    if (!waitForExit)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardError = true;
                    }
                    process.StartInfo.Arguments = arguments.Trim();
                }

                // Runing non executable files.
                if (exe)
                {
                    process.Start();
                    if (waitForExit)
                        process.WaitForExit();
                    return;
                }

                if (fileCheck)
                {
                    if (File.Exists(input))
                    {
                        process.Start();
                        if (waitForExit)
                            process.WaitForExit();
                        return;
                    }
                    FileSystem.ErrorWriteLine($"Couldn't find file \"{input}\" to execute");
                    return;
                }
                process.Start();
                if (waitForExit)
                    process.WaitForExit();
            }
            catch (System.ComponentModel.Win32Exception win)
            {
                if (win.Message.Contains("The user name or password is incorrect."))
                    FileSystem.ErrorWriteLine("The user name or password is incorrect!");
                else
                    FileSystem.ErrorWriteLine(win.Message);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Process execution in separate window with no args.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="asAdmin"></param>
        public static void ProcessExecute(string input, bool asAdmin)
        {
            try
            {
                var process = new Process();

                if (asAdmin)
                {
                    process.StartInfo = new ProcessStartInfo(input)
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                }
                else
                {
                    process.StartInfo = new ProcessStartInfo(input)
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    };
                }
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception win)
            {
                FileSystem.ErrorWriteLine(win.Message);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Execute comand promt or powershell with params.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="args"></param>
        public static void Execute(string input, string args)
        {
            s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
            if (input.StartsWith("cmd"))
            {
                args = args.Split(' ').Count() >= 1 ? args.Replace("cmd ", "/c ") : args.Replace("cmd", "");
                ExecutApp("cmd", args, true);
                return;
            }
            if (input.StartsWith("ps"))
            {
                args = args.Split(' ').Count() >= 1 ? args.Replace("ps", "") : args.Replace("ps ", "");
                ExecutApp("powershell", args, true);
                return;
            }
        }


        private static void ExecutApp(string processName, string arg, bool waitForExit)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(processName)
            {
                UseShellExecute = false,
                WorkingDirectory = s_currentDirectory,
                Arguments = arg
            };
            process.Start();
            if (waitForExit)
                process.WaitForExit();
        }

        /// <summary>
        /// Run application and show output.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static StreamReader ExecuteAppWithOutput(string app, string arguments = "")
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.UseShellExecute = false;
            processStartInfo.Arguments = arguments;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.FileName = app;
            processStartInfo.CreateNoWindow = true;
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            return process.StandardOutput;
        }
    }
}
