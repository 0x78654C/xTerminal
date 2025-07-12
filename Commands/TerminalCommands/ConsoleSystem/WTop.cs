using Core;
using Core.SystemTools;
using System;
using System.Runtime.Versioning;


namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class WTop : ITerminalCommand
    {
        public string Name => "wtop";
        private static string s_helpMessage = @"Usage of wtop command:
 -h: Display this help message.

Inside the wtop command:
    q: Quit the wtop interface.

";

        public void Execute(string arg)
        {
            try
            {
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                if (arg.Length == 5)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                var wtop = new ProcessListingUI();
                wtop.Run();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
                return;
            }
        }
    }
}
