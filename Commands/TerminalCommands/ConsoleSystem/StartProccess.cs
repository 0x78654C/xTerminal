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

            args = args.Replace("start ", "");
            string param = args.Split(' ').First();
            if (args.Contains(@"\"))
            {
                if (param == "-u")
                {
                    args = args.Replace("-u ", "");
                    StartApplication(args, true);
                }
                else
                {
                    StartApplication(args, false);
                }
            }
            else
            {
                if (param == "-u")
                {
                    args = args.Replace("-u ", "");
                    StartApplication(s_currentDirectory + @"\\" + args, true);
                }
                else
                {
                    StartApplication(s_currentDirectory + @"\\" + args, false);
                }
            }
        }

        private void StartApplication(string inputCommand, bool admin)
        {

            try
            {
                string[] dInput = inputCommand.Split(' ');
                int _ch = Regex.Matches(inputCommand, " ").Count;

                if (_ch == 1)
                {
                    if (!File.Exists(dInput[0]))
                    {
                        FileSystem.ErrorWriteLine($"File {dInput[0]} does not exist!");
                        return;
                    }
                    if (admin)
                    {
                        Core.SystemTools.ProcessStart.ProcessExecute(dInput[0], dInput[1], true, true);
                        return;
                    }
                    Core.SystemTools.ProcessStart.ProcessExecute(dInput[0], dInput[1], true, false);
                }
                else
                {
                    if (!File.Exists(dInput[0]))
                    {
                        FileSystem.ErrorWriteLine($"File {dInput[0]} does not exist!");
                        return;
                    }
                    if (admin)
                    {
                        Core.SystemTools.ProcessStart.ProcessExecute(dInput[0], "", true, true);
                        return;
                    }
                    Core.SystemTools.ProcessStart.ProcessExecute(dInput[0], "", true, false);
                }

            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
