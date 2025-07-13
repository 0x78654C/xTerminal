/*
 
    Return MD5, SHA256 and SHA512 of a file.
    
 */

using Core;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.Encription
{
    [SupportedOSPlatform("Windows")]
    public class Hash : ITerminalCommand
    {
        private static string s_helpMessage = @"Usage of hash command:
    hash <file_path>         : display the MD5 hash for the file.
    hash -sha256 <file_path> : display the sha256 hash for the file.
    hash -sha512 <file_path> : display the sha512 hash for the file.
";
        public string Name => "hash";
        public void Execute(string args)
        {
            var s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

            GlobalVariables.isErrorCommand = false;
            if (args.Trim() == Name && !GlobalVariables.isPipeCommand)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            if (args == $"{Name} -h" && !GlobalVariables.isPipeCommand)
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            try
            {
                string[] arg = args.Split(' ');
                if (arg.ContainsParameter("-sha256"))
                {

                    var file = FileSystem.SanitizePath(arg.ParameterAfter("-sha256").Trim(), s_currentDirectory);
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        file = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory);
                    if (!File.Exists(file))
                    {
                        FileSystem.ErrorWriteLine($"File does not exist: {file}");
                        return;
                    }

                    var hash256 = Core.Encryption.HashAlgo.GetSHA256(file);
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput = hash256;
                    else
                        FileSystem.SuccessWriteLine($"SHA256: {hash256}");
                    return;
                }

                if (arg.ContainsParameter("-sha512"))
                {

                    var file = FileSystem.SanitizePath(arg.ParameterAfter("-sha512").Trim(), s_currentDirectory);
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        file = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory);
                    if (!File.Exists(file))
                    {
                        FileSystem.ErrorWriteLine($"File does not exist: {file}");
                        return;
                    }

                    var hash512 = Core.Encryption.HashAlgo.GetSHA512(file);
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                        GlobalVariables.pipeCmdOutput = hash512;
                    else
                        FileSystem.SuccessWriteLine($"SHA256: {hash512}");
                    return;
                }

                var filePath = FileSystem.SanitizePath(arg.ParameterAfter("hash").Trim(), s_currentDirectory);
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    filePath = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), s_currentDirectory);
                if (!File.Exists(filePath))
                {
                    FileSystem.ErrorWriteLine($"File does not exist: {filePath}");
                    return;
                }

                var hashMD5 = Core.Encryption.HashAlgo.GetMD5Hash(filePath);
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput = hashMD5;
                else
                    FileSystem.SuccessWriteLine($"MD5: {hashMD5}");
                return;
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.ToString());
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
