using Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Command for display running background commands.
     */


    [SupportedOSPlatform("Windows")]
    public class BGCommandsManage : ITerminalCommand
    {
        public string Name => "bc";
        private string _backgroundCommandsPidList = GlobalVariables.bgProcessListFile;
        private static string s_helpMessage = @"Usage of bc command parameters:

    bc : Display running background commands.
 
Note: Background commands are killed when xTerminal is closed!
";
        public void Execute(string arg)
        {
            GlobalVariables.isErrorCommand = false;
            if (arg == Name)
            {
                if (File.Exists(_backgroundCommandsPidList))
                {
                    var listBgRemain = "";
                    var readBGList = File.ReadAllLines(_backgroundCommandsPidList);
                    File.WriteAllText(_backgroundCommandsPidList, string.Empty);
                    if (readBGList.Length == 0)
                        return;
                    FileSystem.SuccessWriteLine("List of running background commands:");
                    FileSystem.SuccessWriteLine("------------------------------------");
                    foreach (var line in readBGList)
                    {
                        var splitPid = Int32.Parse(line.Split("PID: ")[1]);
                        var isActive = Process.GetProcesses().Any(p => p.Id == splitPid);
                        if (!isActive)
                            continue;
                        if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                            GlobalVariables.pipeCmdOutput += line+Environment.NewLine;
                        else
                            FileSystem.SuccessWriteLine(line);
                        listBgRemain += line+Environment.NewLine;
                    }
                    FileSystem.SuccessWriteLine("------------------------------------");
                    File.WriteAllText(_backgroundCommandsPidList, listBgRemain);
                }
                else
                    FileSystem.ErrorWriteLine("Somewith went wrong on reading the background commands list!");
                return;
            }

            // Display help message.
            if (arg.Trim() == "-h" && !GlobalVariables.isPipeCommand)
            {
                Console.WriteLine(s_helpMessage);
                return;
            }
        }
    }
}
