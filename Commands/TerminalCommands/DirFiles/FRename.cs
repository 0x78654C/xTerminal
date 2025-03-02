using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class FRename : ITerminalCommand
    {
        /*
         File/directory rename.
         */
        public string Name => "mv";
        private static string s_helpMessage = @"Usage of mv locate command:

    Renames a file or directory:

    Example: mv <old_name_file/dir> -o <new_name_file/dir> 

";

        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                arg = arg.Replace("mv ", "");


                if (arg.StartsWith("-h") && arg.Length == 2)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // Reading current location(for test no,after i make dynamic)
                string dlocation = File.ReadAllText(GlobalVariables.currentDirectory); ;

                // We grab the file names for source and destination
                string fileName = string.Empty;
                string newName = string.Empty;
                if (GlobalVariables.isPipeCommand)
                    fileName = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), dlocation);
                else
                    fileName = FileSystem.SanitizePath(arg.SplitByText(" -o ", 0), dlocation);
                if (GlobalVariables.isPipeCommand)
                    newName = FileSystem.SanitizePath(arg.SplitByText("-o ", 1), dlocation);
                else
                    newName = FileSystem.SanitizePath(arg.SplitByText(" -o ", 1), dlocation);

                // We check if file exist.
                if (File.Exists(fileName))
                {
                    File.Move(fileName, newName);
                    FileSystem.SuccessWriteLine($"File renamed from {fileName} to {newName}");
                    return;
                }

                // We check if directory exist.
                if (Directory.Exists(fileName))
                {
                    Directory.Move(fileName, newName);
                    FileSystem.SuccessWriteLine($"Directory renamed from {fileName} to {newName}");
                    return;
                }
                FileSystem.ErrorWriteLine("File/directory " + fileName + " does not exist!");
                GlobalVariables.isErrorCommand = true;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
