using Core;
using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")] 
    public class Zip : ITerminalCommand
    {
        public string Name => "zip";
        private static string s_helpMessage = @"Usage of ln command parameters:
    ln <path_file_folder> : Create shortcut of a specific file/directory on Desktop.
    ln <path_file_folder> -o <path_location_shortcut> : Create shortcut in a specific location.
";
        public void Execute(string arg)
        {
            try
            {
                // No parameter.
                if (arg == Name)
                {
                    FileSystem.SuccessWriteLine("Use -h for more information!");
                    return;
                }

                arg = arg.Substring(3);

                // Display help message.
                if (arg.Trim() == "-h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                var zipManager = new ZipManager();
                if (arg.Trim().StartsWith("-list "))
                {
                    var archiveFile = arg.SplitByText("-list ",1).Trim();
                    zipManager.ZipName = archiveFile;
                    zipManager.List();
                    return;
                }

                if (arg.Trim().Contains(" -n "))
                {
                    var archiveFile = arg.SplitByText(" -n ", 1).Trim();
                    var filesToBeArchived = arg.SplitByText(" -n ", 0).Trim();
                    zipManager.ZipName = archiveFile;
                    zipManager.ZipDir = filesToBeArchived;
                    zipManager.Archive();
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
            }
        }
    }
}
