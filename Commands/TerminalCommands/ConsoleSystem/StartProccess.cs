using Core;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class StartProccess : ITerminalCommand
    {
        private static string s_currentDirectory = string.Empty;
        public string Name => "start";
        private string _helpMessage = @"Usage: start <file_name> OR start <file_name> -p <file_paramters>
Can use with following parameter:
    -h : Display this message.
    -u : Can run process with different user.
         Example1: start -u <file_name>
         Example2: start -u <file_name> -p <file_paramters>
";

        public void Execute(string args)
        {

            // Set directory, to be used in other functions
            s_currentDirectory =
                            File.ReadAllText(GlobalVariables.currentDirectory);
            if (args.Length == 5)
            {
                Console.WriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            int argsLength = args.Length - 6;
            args = args.Substring(6, argsLength);
            string param = args.Split(' ').First();
            string paramApp = string.Empty;

            if (args.Contains("-p"))
            {
                paramApp = args.SplitByText(" -p ", 1);
                args = args.SplitByText(" -p ", 0);
            }
            if (args.StartsWith("-h"))
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

                    StartApplication(args, paramApp, true);
                    return;
                }
                StartApplication(args, paramApp, false);
                return;
            }
            args = FileSystem.SanitizePath(args, s_currentDirectory);

            if (param == "-u")
            {
                args = args.Replace("-u ", "");
                StartApplication(args, paramApp, true);
                return;
            }
            StartApplication(args, paramApp, false);
        }

        /// <summary>
        /// Start process using args and other user options.
        /// </summary>
        /// <param name="inputCommand">Path to procces required to be started.</param>
        /// <param name="arg">Arguments</param>
        /// <param name="admin">Use other user for run procces.</param>
        private void StartApplication(string inputCommand, string arg, bool admin)
        {
            try
            {
                int _ch = Regex.Matches(inputCommand, " ").Count;

                if (!File.Exists(inputCommand))
                {
                    FileSystem.ErrorWriteLine($"File {inputCommand} does not exist!");
                    return;
                }
                if (admin)
                {
                    Core.SystemTools.ProcessStart.ProcessExecute(inputCommand, arg, true, true);
                    return;
                }
                Core.SystemTools.ProcessStart.ProcessExecute(inputCommand, arg, true, false);
                return;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
