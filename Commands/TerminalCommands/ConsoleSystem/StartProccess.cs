using Core;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class StartProccess : ITerminalCommand
    {
        private static string s_currentDirectory = string.Empty;
        public string Name => "./";
        private string _helpMessage = @"Usage: ./ <file_name> OR ./ <file_name> -param <file_paramters>
Can be used with the following parameters:
    -h    : Displays this message.
    -u    : Can run process with different user.
    -we   : Disable wait for process to exit.
    -wi   : Show new process window.
    -param: Start process with specified parameters.
         Example1: ./ -u <file_name>
         Example2: ./ -u <file_name> -param <file_paramters>
Both examples can be used with -we parameter.
";

        public void Execute(string args)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                // Set directory, to be used in other functions
                s_currentDirectory =
                                File.ReadAllText(GlobalVariables.currentDirectory);
                if (args == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                int argsLength = args.Length - 3;
                args = args.Substring(3, argsLength);
                string param = args.Split(' ').First();
                string paramApp = string.Empty;
                bool waitForExit = false;

                if(args.Contains("-wi"))
                {
                    args = args.Replace(" -wi".Trim(), string.Empty);
                    waitForExit = true;
                }

                if (args.Contains("-we"))
                {
                    args = args.Replace(" -we".Trim(), string.Empty);
                    waitForExit = true;
                }

                if (args.Contains("-param"))
                {
                    paramApp = args.SplitByText(" -param ", 1);
                    args = args.SplitByText(" -param ", 0);
                }
                if (args == "-h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }
                if (args.Contains(@"\"))
                {
                    if (param == "-u")
                    {
                        args = args.Replace("-u ", "");
                        args = FileSystem.SanitizePath(args, s_currentDirectory);

                        StartApplication(args, paramApp, true, !waitForExit);
                        return;
                    }

                    args = FileSystem.SanitizePath(args, s_currentDirectory);
                    StartApplication(args, paramApp, false, !waitForExit);
                    return;
                }
                args = FileSystem.SanitizePath(args, s_currentDirectory);

                if (param == "-u")
                {
                    args = args.Replace("-u ", "");
                    StartApplication(args, paramApp, true, !waitForExit);
                    return;
                }

                StartApplication(args, paramApp, false, !waitForExit);
            }
            catch (Exception e)
            { 
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Start process using args and other user options.
        /// </summary>
        /// <param name="inputCommand">Path to procces required to be started.</param>
        /// <param name="arg">Arguments</param>
        /// <param name="admin">Use other user for run procces.</param>
        private void StartApplication(string inputCommand, string arg, bool admin, bool waitForExit)
        {
            try
            {
                int _ch = Regex.Matches(inputCommand, " ").Count;

                if (File.Exists(inputCommand + ".exe"))
                    inputCommand = inputCommand + ".exe";

                if (File.Exists(inputCommand + ".msi"))
                    inputCommand = inputCommand + ".msi";

                if (!File.Exists(inputCommand))
                {
                    FileSystem.ErrorWriteLine($"File {inputCommand} does not exist!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                if (admin)
                {
                    Core.SystemTools.ProcessStart.ProcessExecute(inputCommand, arg, true, true, waitForExit, bool isWindow);
                    return;
                }
                Core.SystemTools.ProcessStart.ProcessExecute(inputCommand, arg, true, false, waitForExit);
                return;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
