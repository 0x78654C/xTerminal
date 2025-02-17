using Core;
using Core.SystemTools;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class FileShred : ITerminalCommand
    {
        public string Name => "shred";
        private Shred _shred;
        private static string s_helpMessage = @"Usage of shred command:
  shred <file_path> :   Will shred the file with the default of 3 passes.
  shred <file_path> -i <number_of_passes> :   Will shred the file with the specified number!
";

        public void Execute(string args)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
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

                var currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

                if (args.Contains(" -i "))
                {
                    int passes = Int32.Parse(args.SplitByText("-i ", 1).Trim());
                    string filePath =  string.Empty;
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        filePath = GlobalVariables.pipeCmdOutput.Trim();
                    else
                        filePath = args.SplitByText(" -i", 0).Trim();
                    string fileSanitizeI = FileSystem.SanitizePath(filePath, currentDirectory);
                    if (!IsShreding()) return;
                    _shred = new Shred(fileSanitizeI, passes);
                    _shred.ShredFile();
                    return;
                }

                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    args = GlobalVariables.pipeCmdOutput.Trim();
                else
                    args = args.Replace("shred ", String.Empty);
                string fileSanitize = FileSystem.SanitizePath(args, currentDirectory);
                if (!IsShreding()) return;
                _shred = new Shred(fileSanitize, 0);
                _shred.ShredFile();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}\nUse -h param for {{Name}} command usage!");
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Confirmation for file shred.
        /// </summary>
        /// <returns></returns>
        private static bool IsShreding()
        {
            Console.Write("Do you really want to shred the file? Yes(Y) No(N):");
            var key = Console.ReadKey().Key.ToString().ToLower();
            var isShreding = false;
            switch (key)
            {
                case "y":
                    isShreding = true;
                    Console.Write("\n");
                    break;
                case "n":
                    isShreding = false;
                    FileSystem.SuccessWriteLine("\nFile shred procees stoped!");
                    break;
                default:
                    isShreding = false;
                    FileSystem.SuccessWriteLine("\nFile shred procees stoped!");
                    break;
            }
            return isShreding;
        }
    }
}
