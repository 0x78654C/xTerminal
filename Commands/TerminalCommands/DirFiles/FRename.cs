using Core;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
    public class FRename : ITerminalCommand
    {
        public string Name => "frename";

        public void Execute(string arg)
        {
            try
            {
                string[] input = arg.Split(' ');

                //reading current location(for test no, after i make dynamic)
                string dlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ;
                string cLocation = Directory.GetCurrentDirectory();
                //we grab the file names for source and destination
                string FileName = input[1];
                string NewName = input[2];

                //we check if is a diference between the too directories and see if is the current one set
                if (dlocation == cLocation || FileName.Contains(@"\"))
                {
                    //we check if file exists
                    if (File.Exists(FileName))
                    {
                        File.Move(FileName, NewName);
                    }
                    else
                    {
                        FileSystem.ErrorWriteLine("File " + FileName + " dose not exist!");
                    }
                }
                else
                {
                    //we check if file exists
                    if (File.Exists(dlocation + @"\" + FileName))
                    {
                        File.Move(dlocation + @"\" + FileName, dlocation + @"\" + NewName);
                    }
                    else
                    {
                        FileSystem.ErrorWriteLine("File " + dlocation + @"\" + FileName + " dose not exist!");
                    }
                }
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file name!");
            }
        }
    }
}
