using Core;
using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("windows")]
    public class Exif: ITerminalCommand
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
    exif <path_to_JPEG/JPG_file>. 
    
    Can be used with the following parameters:
    exif -h : Displays this message.

Attention: Works only with the following extensions: {string.Join(", ",s_imgExt)}.
";
        public void Execute(string args)
        {
            try
            {
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                var pathFile = args.Replace($"{Name}","").Trim();
                if (File.Exists(pathFile))
                {

                    var fileInfo = new FileInfo(pathFile);
                    if (s_imgExt.Contains(fileInfo.Extension.ToLower()))
                        ExifLib.GetExifInfo(fileInfo.FullName);
                    else
                        FileSystem.ErrorWriteLine($"Format '{fileInfo.Extension}' unsupported! Use -h for check avaible formats.");
                }
                else
                {
                    FileSystem.ErrorWriteLine($"File does not exist: '{pathFile}'");
                }
            }
            catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
            
        }
    }
}
