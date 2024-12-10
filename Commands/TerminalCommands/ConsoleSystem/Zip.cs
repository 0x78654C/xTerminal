using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class Zip : ITerminalCommand
    {
        public string Name => "zip";
        private static string s_helpMessage = @"Usage of ln command parameters:
    ln <path_file_folder> : Create shortcut of a specific file/directory on Desktop.
    ln <path_file_folder> -o <path_location_shortcut> : Create shortcut in a specific location.
";
        public void Execute(string arg)
        {
        }
    }
}
