using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Core.SystemTools
{
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

        private static string GetExecutablePath(string executableFilePath)  => Path.GetDirectoryName(executableFilePath);
       
        public static void ProcessExecute(string input, string arguments, bool fileCheck, bool asAdmin, bool waitForExit)
        {
            try
            {
                var process = new Process();

                if (asAdmin)
                {
                    process.StartInfo = new ProcessStartInfo(input);
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.WorkingDirectory = GetExecutablePath(input);
                    process.StartInfo.UseShellExecute = false;
                    if (!waitForExit)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardError = true;
                    }
                    process.StartInfo.Verb = "runas";
                }
                else
                {
                    process.StartInfo = new ProcessStartInfo(input);
                    process.StartInfo.WorkingDirectory = GetExecutablePath(input);
                    process.StartInfo.UseShellExecute = false;
                    if (!waitForExit)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardError = true;
                    }
                    process.StartInfo.Arguments = arguments;
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
    }
}
