using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Core;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     * Start new terminal window.
     */
    [SupportedOSPlatform("Windows")]
    class NewTWindow : ITerminalCommand
    {
        public string Name => "nt";
        private static string s_helpMessage = @"Usage of nt command:
    nt    : Opens new terminal window.
    nt -u : Opens new terminal window as administrator.
";
        public void Execute(string args)
        {

            if (args ==Name && !args.Contains("-u"))
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            if (args == $"{Name} -h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }
            if (args.ContainsText("-u"))
            {
                Core.SystemTools.ProcessStart.ProcessExecute(Application.StartupPath + "\\xTerminal.exe", true);
                return;
            }
            Core.SystemTools.ProcessStart.ProcessExecute(Application.StartupPath + "\\xTerminal.exe", false);
        }
    }
}
