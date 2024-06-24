using Core;
using Core.SystemTools;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class HexViewer : ITerminalCommand
    {
        /*
         * Simple hex viewer.
         */

        public string Name => "hex";
        private static string s_currentDirectory;
        private static string s_helpMessage = @"Usage of hex command:
 hex <file_name> : Display the memory hex view of the provided file.
 hex <file_name> -o <output_file_name>: Saves the memory hex view of the provided file.
";
        public void Execute(string args)
        {
            string file = "";
            try
            {
                s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                var arg = args.Split(' ');

                if (args == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                } 
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    args = args.Replace("hex", GlobalVariables.pipeCmdOutput.Trim());

                if (arg.ContainsParameter("-o"))
                {
                    string fileToSave = string.Empty;
                    string dumpFile1=string.Empty;
                    string dumpFile = string.Empty;
                    if (!GlobalVariables.isPipeCommand)
                    {
                        dumpFile1 = args.Replace("hex ", string.Empty);
                        dumpFile = dumpFile1.SplitByText(" -o", 0);
                        file = FileSystem.SanitizePath(dumpFile, s_currentDirectory);
                        fileToSave = FileSystem.SanitizePath(args.SplitByText(" -o ", 1), s_currentDirectory);
                    }
                    else{
                        file = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory);
                        var argParseFileToSave = args.SplitByText(" -o ", 1);
                        fileToSave = FileSystem.SanitizePath(argParseFileToSave, s_currentDirectory);
                    }
                    HexDumpFile(file, true, fileToSave);
                    return;
                }
                args = args.Replace("hex", string.Empty).Trim();
                file = FileSystem.SanitizePath(args, s_currentDirectory);
                HexDumpFile(file, false, "");
            }
            catch (UnauthorizedAccessException)
            {
                FileSystem.ErrorWriteLine($"You need administrator rights to save file in: {file} ");
            }
            catch (OutOfMemoryException)
            {
                FileSystem.ErrorWriteLine($"Ran out of memmory!");
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Outputs or saves the hex dump to a file.
        /// </summary>
        /// <param name="file">Path of file for hexdump.</param>
        /// <param name="saveToFile">True for save output to a file.</param>
        /// <param name="savePath">File path and name where to store the dump.</param>
        private static void HexDumpFile(string file, bool saveToFile, string savePath)
        {
            if (File.Exists(file))
            {
                byte[] getBytes = File.ReadAllBytes(file);
                if (saveToFile)
                {
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount != GlobalVariables.pipeCmdCountTemp)
                    {
                        FileSystem.ErrorWriteLine("You cannot save to file when using with pipe command!");
                        return;
                    }
                    Console.WriteLine(FileSystem.SaveFileOutput(savePath, s_currentDirectory, HexDump.Hex(getBytes, 16), true));
                    return;
                }
                if (GlobalVariables.isPipeCommand)
                {
                    GlobalVariables.pipeCmdOutput = HexDump.Hex(getBytes, 16);
                    return;
                }
                else
                    Console.WriteLine(HexDump.Hex(getBytes, 16));
                return;
            }
            FileSystem.ErrorWriteLine($"File {file} does not exist");
        }
    }
}
