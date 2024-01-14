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
        private string _helpMessage = @"TODO: work in progress.
";


        public void Execute(string args)
        {
            try
            {
                if (args.Length == 5)
                //{
                //    Console.WriteLine($"Use -h param for {Name} command usage!");
                //    return;
                //}

                // Main run
                ListProcesses.GetProcessList();

            }catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message); // We live like this till finish command.
            }
        }

    }
}
