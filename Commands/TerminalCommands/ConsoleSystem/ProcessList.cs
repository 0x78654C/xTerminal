using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Core.SystemTools;
namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class ProcessList : ITerminalCommand
    {

        private static string s_currentDirectory = string.Empty;
        public string Name => "plist";
        private string _helpMessage = @"List current running processes and their child process.
";


        public void Execute(string args)
        {
            try
            {
                if (args == "-h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }
                if (args.Length == 5)
                //{
                //    Console.WriteLine($"Use -h param for {Name} command usage!");
                //    return;
                //}

                // Main run

                ListProcesses.GetProcessList();

            }catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString()); // We live like this till finish command.
            }
        }

    }
}
