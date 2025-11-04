/*
 
SSH Command wrapper for terminal. 

 */

using Core;
using System;
using System.Runtime.Versioning;
using SystemCmd = Core.Commands.SystemCommands;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class SSH : ITerminalCommand
    {
        public string Name => "ssh";
        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;

                if (arg == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (arg == $"{Name} -h")
                {
                    SystemCmd.SSHCmd(string.Empty);
                    return;
                }
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    arg = arg.Replace("ssh", GlobalVariables.pipeCmdOutput.Trim());
                else
                    arg = arg.Substring(3).Trim();
                SystemCmd.SSHCmd(arg);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
