using System;
using System.Windows.Forms;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     * Start new terminal window.
     */

    class NewTWindow : ITerminalCommand
    {
        public string Name => "nt";
        private static string s_helpMessage = @"Usage of nt command:
    nt    : Opens new terminal window.
    nt -u : Opens new terminal window as administrator.
";
        public void Execute(string args)
        {

            if (args.Length > 2 && !args.Contains("-u"))
            {
                Console.WriteLine($"Use -h param for {Name} command usage!");
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
