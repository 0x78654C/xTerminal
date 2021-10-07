using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;
using System.IO;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class HexViewer : ITerminalCommand
    {
        public string Name => "hex";
        private static string s_currentDirectory;
        public void Execute(string args)
        {
            s_currentDirectory = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
            var arg = args.Split(' ');
            string file = FileSystem.SanitizePath(arg.ParameterAfter("hex"), s_currentDirectory);
            if (arg.ContainsParameter("-o"))
            {
                try
                {
                    HexDumpFile(file, true, arg.ParameterAfter("-o"));
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
            HexDumpFile(file,false,"");
        }

        private static void HexDumpFile(string file, bool saveToFile,string savePath)
        {
            if (File.Exists(file))
            {
               
                byte[] getBytes = File.ReadAllBytes(file);
                if (saveToFile) {
                    Console.WriteLine(FileSystem.SaveFileOutput(savePath, s_currentDirectory, HexDump.Hex(getBytes, 16)));
                }
                else
                {
                    Console.WriteLine(HexDump.Hex(getBytes, 16));
                }
            }
            else
            {
                FileSystem.ErrorWriteLine($"File {file} does not exist");
            }
        }
    }
}
