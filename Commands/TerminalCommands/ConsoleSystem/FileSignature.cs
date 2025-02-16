using System;
using System.Linq;
using System.IO;
using Commands;
using System.Runtime.Versioning;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class FileSignature : ITerminalCommand
    {
        /* File singature (magic numbers) verification class. */
        public string Name => "fsig";
        private bool _isIterated = false;
        private static readonly string s_helpMessage = @"Usage of file extension tool:
 	fsig <file_path>      : Display file path, extension, hex signature, and signature description.
 	fsig <file_path> -ext : Display extension only.
 	fsig -h               : Display this help message.


List must be in same place with xTerminal.exe file and named ext_list.txt
List format: <hex signature>|<extension(s)>|Description
    Example:
4D 5A|exe, scr, sys, dll, fon, cpl, iec, ime, rs, tsp, mz|DOS MZ executable and its descendants (including NE and PE) 

Hex signature list is based on https://en.wikipedia.org/wiki/List_of_file_signatures
";
        private readonly string _extFile = GlobalVariables.magicNunmbers;

        public void Execute(string args)
        {
            try
            {
                var currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

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

                if (!CheckExtFile(_extFile))
                {
                    FileSystem.ErrorWriteLine("File ext_list.txt is not present with file type signatures. File must be located with the xTerminal executable");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                var file = string.Empty;
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    file = $"{GlobalVariables.pipeCmdOutput.Trim()} {args.Replace("fsig", string.Empty).Trim()}";
                else
                    file = args.Replace("fsig",string.Empty).Trim();

                if (file.Trim().EndsWith(" -ext"))
                {
                    var filePath = file.Substring(0, file.Length - 4);
                    var sanitizedFileExt = FileSystem.SanitizePath(filePath, currentDirectory);
                    if (File.Exists(sanitizedFileExt))
                        CheckFileSignature(_extFile, sanitizedFileExt, true);
                    else
                    {
                        FileSystem.ErrorWriteLine($"File {sanitizedFileExt} does not exist!");
                        GlobalVariables.isErrorCommand = true;
                    }
                    return;
                }

                var sanitizedFile = FileSystem.SanitizePath(file, currentDirectory);
                if (File.Exists(sanitizedFile))
                    CheckFileSignature(_extFile, sanitizedFile, false);
                else
                {
                    FileSystem.ErrorWriteLine($"File {sanitizedFile} does not exist!");
                    GlobalVariables.isErrorCommand = true;
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Check if extension file is present.
        /// </summary>
        /// <param name="extFilePath"></param>
        /// <returns></returns>
        private bool CheckExtFile(string extFilePath) => File.Exists(extFilePath);


        /// <summary>
        /// Check file signature by magic numbers  compared to a whitelist of singatures.
        /// </summary>
        /// <param name="extFileName"></param>
        /// <param name="filePath"></param>
        /// <param name="extOnly"></param>
        private void CheckFileSignature(string extFileName, string filePath, bool extOnly)
        {
            var outMessage = "Unknown signature.";
            var extLines = File.ReadAllLines(extFileName);
            var hexFile = HexDump.GetHex(filePath);
            var fileInfo = new FileInfo(filePath);
            var nameExt = fileInfo.Extension.Replace(".", string.Empty);
            var isExtFound = false;
            foreach (var line in extLines.Where(line => !string.IsNullOrEmpty(line)))
            {
                var hex = line.Split('|')[0];
                var check = hexFile.Contains(hex);
                var ext = line.Split('|')[1];
                var description = line.Split('|')[2];

                if (!check || (!_isIterated && !ext.Contains(nameExt))) continue;
                isExtFound = true;
                outMessage = extOnly
                    ? ext
                    : $@"
-------------------------------------------------------------	
File:          {filePath}
Extension(s):  {ext}
Hex signature: {hex}
Description:   {description}
-------------------------------------------------------------";
                break;
            }
            if (isExtFound)
            {
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    GlobalVariables.pipeCmdOutput = $"{outMessage}\n";

                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                {
                    GlobalVariables.pipeCmdOutput = $"{outMessage}\n";
                }
                else if (GlobalVariables.pipeCmdCount == GlobalVariables.pipeCmdCountTemp)
                {
                    GlobalVariables.pipeCmdOutput = $"{outMessage}\n";
                }

                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                    Console.WriteLine(outMessage);

                if (!GlobalVariables.isPipeCommand)
                    Console.WriteLine(outMessage);
            }
            else if (!_isIterated)
            {
                _isIterated = true;
                CheckFileSignature(extFileName, filePath, extOnly);
            }
        }
    }
}
