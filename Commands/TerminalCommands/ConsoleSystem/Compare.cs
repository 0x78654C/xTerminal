using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Compare : ITerminalCommand
    {
        public string Name => "cmp";
        private string s_currentDirectory;
        private static string s_helpMessage = @"Usage of cmp command:
    cmp <firstFile>;<secondFile> : Check if two files are identical. 
";
        public void Execute(string args)
        {
            s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
            try
            {
                GlobalVariables.isErrorCommand = false;
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (args == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                args = args.Replace(Name, "");
                var splitArgs = args.Split(';');
                var firtFile = splitArgs[0].Trim();
                var secondFile = splitArgs[1].Trim();
                CompareFiles(firtFile, secondFile);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Compare files md5 hash.
        /// </summary>
        /// <param name="firstFile"></param>
        /// <param name="secondFile"></param>
        public void CompareFiles(string firstFile, string secondFile)
        {
            firstFile = FileSystem.SanitizePath(firstFile.Trim(), s_currentDirectory);
            secondFile = FileSystem.SanitizePath(secondFile.Trim(), s_currentDirectory);

            if (!File.Exists(firstFile))
            {
                Console.WriteLine($"File does not exist: {firstFile}");
                return;
            }

            if (!File.Exists(secondFile))
            {
                Console.WriteLine($"File does not exist: {secondFile}");
                return;
            }

            var firstMD5 = Core.Encryption.HashAlgo.GetMD5Hash(firstFile);
            var secondMD5 = Core.Encryption.HashAlgo.GetMD5Hash(secondFile);
            if (firstMD5 == secondMD5)
                FileSystem.SuccessWriteLine("Files are identical!");
            else
                Console.WriteLine("Files are NOT identical");
        }
    }
}
