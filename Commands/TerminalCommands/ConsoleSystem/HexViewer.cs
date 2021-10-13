using Core;
using System;
using System.IO;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class HexViewer : ITerminalCommand
    {
        /*
         * Simple hex viewer.
         */

        public string Name => "hex";
        private static string s_currentDirectory;
        public void Execute(string args)
        {
            s_currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
            var arg = args.Split(' ');
            string file;
            if (arg.ContainsParameter("-o"))
            {
                file = FileSystem.SanitizePath(arg.ParameterAfter("hex"), s_currentDirectory);
                try
                {
                    HexDumpFile(file, true, args.SplitByText(" -o ",1));
                }
                catch (UnauthorizedAccessException)
                {
                    FileSystem.ErrorWriteLine($"You need administrator rights to save file in: {arg.ParameterAfter("-o")} ");
                }
                catch (OutOfMemoryException)
                {
                    FileSystem.ErrorWriteLine($"Ran out of memmory!");
                }

                return;
            }
            int argLength = args.Length - 4;
            file = args.Substring(3, argLength);
            HexDumpFile(file, false, "");
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
                    Console.WriteLine(FileSystem.SaveFileOutput(savePath, s_currentDirectory, HexDump.Hex(getBytes, 16), true));
                    return;
                }
                Console.WriteLine(HexDump.Hex(getBytes, 16));
                return;
            }

            FileSystem.ErrorWriteLine($"File {file} does not exist");
        }
    }
}
