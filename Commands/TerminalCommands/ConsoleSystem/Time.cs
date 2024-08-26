using Core;
using System;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
        Display current time.
    */

    [SupportedOSPlatform("Windows")]
    public class Time: ITerminalCommand
    {
        public string Name => "time";
        public void Execute(string arg)
        {
            var now = DateTime.Now.ToString("HH:mm:ss dd-MM-yyyy");
            FileSystem.SuccessWriteLine(now); 
        }
    }
}
