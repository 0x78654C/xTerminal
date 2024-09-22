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
        private static string s_currentDirectory;
        private static string _cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

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

                //process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
                //process.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;
                //process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                bool exe = input.Trim().EndsWith(".exe") || input.Trim().EndsWith(".msi");


                if (asAdmin)
                {
                    var arg = arguments;
                    var inp = input;
                    if (!string.IsNullOrEmpty(arguments) && arguments.Contains(" "))
                    {
                        arg = $"\"{arguments}\"";
                        arguments = "/c " + "\"" + input + "\"";
                    }
                    else
                        arguments = "/c " + "\"" + input + "\"" + $" {arg}";
                    process.StartInfo = new ProcessStartInfo();
                    process.StartInfo.FileName = _cmdPath;
                    process.StartInfo.Verb = "runas";
                    var secureString = new System.Security.SecureString();
                    if (!exe)
                        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(input);
                    process.StartInfo.Arguments = arguments.Trim();
                    process.StartInfo.UseShellExecute = false;
                    if (!waitForExit)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                    }
                }
                else
                {
                    var fileName = input;

                    if (!exe)
                    {
                        if (string.IsNullOrEmpty(arguments))
                        {
                            if (input.Contains(" "))
                                arguments = $@"/c ""{input}""";
                            else
                                arguments = $@"/c {input}";
                        }
                        else
                        {
                            var inp = input;
                            if (input.Contains(" "))
                                inp = $"\"{input}\"";
                            var arg = arguments;
                            if (arguments.Contains(" "))
                                arg = $"\"{arguments}\"";
                            arguments = $@"/c {inp} {arg}";
                        }
                        fileName = _cmdPath;
                    }
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(input);
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Arguments = arguments;
                    if (!waitForExit)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                    }
                }

                // Runing non executable files.
                if (!exe)
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
        /// Open current directory.
        /// </summary>
        /// <param name="path"></param>
        public static void OpenDirProc(string path)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("explorer");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = path.Trim();
            process.Start();
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
                    Console.Write("User name: ");
                    var userName = Console.ReadLine();
                    if (string.IsNullOrEmpty(userName))
                    {
                        Console.WriteLine();
                        FileSystem.ErrorWriteLine("User name must be provieded!");
                        return;
                    }
                    Console.WriteLine(input);
                    Console.ReadLine();
                    var arg = $"/c runas /user:{userName} {input}";
                    process.StartInfo = new ProcessStartInfo(_cmdPath)
                    {
                        Arguments = arg,
                        UseShellExecute = true
                    };
                    process.Start();
                }
                else
                {
                    process.StartInfo = new ProcessStartInfo(input)
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    };
                    process.Start();
                }
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
