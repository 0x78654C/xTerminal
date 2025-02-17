using Core;
using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("windows")]
    public class Exif : ITerminalCommand
    {
        public string Name => "exif";
        private static List<string> s_imgExt = new List<string>() {
            ".jpg",
            ".jpeg",
            ".jpe",
            ".tiff",
            ".png",
            ".tff"
        };
        private static string s_helpMessage = $@"Usage of exif command:
    exif <path_to_image_file>. 
    
    Can be used with the following parameters:
    exif -h : Displays this message.

Attention: Works only with the following extensions: {string.Join(", ", s_imgExt)}.
";
        public void Execute(string args)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                if (args == $"{Name} -h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                if (args == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                var currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                var pathFile = string.Empty;
                var param = FileSystem.SanitizePath(args.Replace($"{Name}", "").Trim(), currentDirectory);
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    pathFile = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(),currentDirectory);
                else
                    pathFile= param;

                if (File.Exists(pathFile))
                {

                    var fileInfo = new FileInfo(pathFile);
                    if (s_imgExt.Contains(fileInfo.Extension.ToLower()))
                        ExifLib.GetExifInfo(fileInfo.FullName);
                    else
                    {
                        FileSystem.ErrorWriteLine($"Format '{fileInfo.Extension}' unsupported! Use -h for check avaible formats.");
                        GlobalVariables.isErrorCommand = true;
                    }
                }
                else
                {
                    FileSystem.ErrorWriteLine($"File does not exist: '{pathFile}'");
                    GlobalVariables.isErrorCommand = true;
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
