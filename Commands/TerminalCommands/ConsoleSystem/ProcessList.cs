using Core;
using System;
using System.Runtime.Versioning;
using Core.SystemTools;
namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class ProcessList : ITerminalCommand
    {
        /*
         Get process tree.
        */

        public string Name => "plist";
        private string _helpMessage = @"List current running processes and their child processes in a tree view.
    -h : Display this message.
    Example:
    C:\Users\MrX\Projects\~ $ plist
    ├─ csrss.exe (936)
    └─ wininit.exe (848)
       ├─ services.exe (1140)
       │  ├─ svchost.exe (1348)
       │  │  ├─ WmiPrvSE.exe (4588)
       │  │  ├─ StartMenuExperienceHost.exe (9052)
       │  │  ├─ SearchHost.exe (4192)
       │  │  │  └─ msedgewebview2.exe (12856)
       │  │  │     ├─ msedgewebview2.exe (13108)
       │  │  │     ├─ msedgewebview2.exe (12628)
       │  │  │     ├─ msedgewebview2.exe (12852)
       │  │  │     ├─ msedgewebview2.exe (12876)
       │  │  │     └─ msedgewebview2.exe (8128)
       │  │  ├─ UserOOBEBroker.exe (11412)
       │  │  ├─ RuntimeBroker.exe (11496)
       │  │  ├─ Widgets.exe (11660)
       │  │  ├─ RuntimeBroker.exe (11776)

";

        // Main run
        public void Execute(string args)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }

                ListProcesses.PrintProcessTree();

            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

    }
}
