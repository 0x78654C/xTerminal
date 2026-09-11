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
 -tree: Open in process tree view.

Inside the wtop command:
    q / Esc / Ctrl+C : Quit the wtop interface.
    ↑/↓ : To navigate through the process list.
    PageUp/PageDown, Home/End : Move by a page or jump to the first/last process.
    k   : Kill the selected process.
    /   : Search for a process by name or exact PID.
    F3  : Jump to the next search match. Empty Enter repeats the previous search.
    R   : Refresh process data immediately.
    T / F5 : Toggle flat list and process tree view.
    ←/→ : In tree view, collapse/expand a branch or move to its parent/first child.
    Space : Toggle the selected tree branch.
    C   : Sort processes by CPU usage.
    M   : Sort processes by memory usage.
    N   : Sort processes by name.

Tree view sorts sibling processes together. Search reveals matches in collapsed branches.
K kills only the selected process, including in tree view.

Run with administrator privileges to see all users.
";

        public void Execute(string arg)
        {
            GlobalVariables.isErrorCommand = false;
            try
            {
                string options = arg.Length > Name.Length ? arg[Name.Length..].Trim() : string.Empty;
                if (string.Equals(options, "-h", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                var wtop = new ProcessListingUI(string.Equals(options, "-tree", StringComparison.OrdinalIgnoreCase));
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
