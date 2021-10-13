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

        public void Execute(string args)
        {

            // Set directory, to be used in other functions
            s_currentDirectory =
                RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
            int argsLength = args.Length - 6;
            args = args.Substring(6,argsLength);
            string param = args.Split(' ').First();
            if (args.Contains(@"\"))
            {
                if (param == "-u")
                {
                    args = args.Replace("-u ", "");
                    args = FileSystem.SanitizePath(args, s_currentDirectory);
                    StartApplication(args,"", true);
                    return;
                }
                StartApplication(args,"", false);
                return;
            }
            args = FileSystem.SanitizePath(args, s_currentDirectory);

            if (param == "-u")
            {
                args = args.Replace("-u ", "");
                StartApplication(args,"", true);
                return;
            }
            StartApplication(args,"", false);
        }

        /// <summary>
        /// Start process using args and other user options.
        /// </summary>
        /// <param name="inputCommand">Path to procces required to be started.</param>
        /// <param name="arg">Arguments</param>
        /// <param name="admin">Use other user for run procces.</param>
        private void StartApplication(string inputCommand,string arg, bool admin)
        {
            try
            {
                string[] dInput = inputCommand.Split(' ');
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
