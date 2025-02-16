using Core;
using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.IO;

namespace Commands.TerminalCommands.ConsoleSystem
{  
    /* Set/remove file attributes*/
    
    [SupportedOSPlatform("windows")]
    public class Attributes : ITerminalCommand
    {
        public string Name => "attr";
        private static string s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
        private static string s_helpMessage = @"Usage of attr command:
    
    attr <file/dir_path>  : Displays the current attributes of a file or directory.
    
    Parameters:
      -s <attribute list>  : Sets the attribute/attributes to a file or directory. Attributes needs to be splited by ';' if more then 1 are added.   
      -r <attribute list>  : Remove the attribute/attributes to a file or directory. Attributes needs to be splited by ';' if more then 1 are added.   
    
    List of attributes that can be added/removed: Archive, Directory, Hidden, Normal, ReadOnly, System 
";
        public void Execute(string arg)
        {
            try
            {
                if (arg == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                arg = arg.Replace($"{Name} ", string.Empty);

                if (arg.StartsWith("-h") && arg.Length == 2)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // Set attribute
                if (arg.Contains("-s "))
                {
                    var fileDirName = arg.SplitByText("-s", 0).Trim();
                    if (!IsAttributeFIlePreset(fileDirName, true)) return;
                    var attributes = arg.SplitByText("-s", 1).Trim();
                    if (!IsAttributeFIlePreset(attributes, false)) return;

                    if (attributes.Contains(";"))
                    {
                        var splitAttr = attributes.Split(';').ToList();
                        var setAttribute = new AttributesManage(splitAttr, fileDirName);
                        setAttribute.SetRmoveAttributes(false);
                    }
                    else
                    {
                        var setAttribute = new AttributesManage(new List<string>(), fileDirName);
                        setAttribute.SetRemoveSingle(attributes, FileSystem.SanitizePath(fileDirName, s_currentDirectory),false);
                    }
                    return;
                }

                // Remove attribute
                if (arg.Contains("-r "))
                {
                    var fileDirName = arg.SplitByText("-r", 0).Trim();
                    if (!IsAttributeFIlePreset(fileDirName,true)) return;
                    var attributes = arg.SplitByText("-r", 1).Trim();
                    if (!IsAttributeFIlePreset(attributes,false)) return;

                    if (attributes.Contains(";"))
                    {
                        var splitAttr = attributes.Split(';').ToList();
                        var setAttribute = new AttributesManage(splitAttr, fileDirName);
                        setAttribute.SetRmoveAttributes(true);
                    }
                    else
                    {
                        var setAttribute = new AttributesManage(new List<string>(), fileDirName);
                        setAttribute.SetRemoveSingle(attributes, FileSystem.SanitizePath(fileDirName, s_currentDirectory),true);
                    }
                    return;
                }

                // List attributes
                var fileDir = FileSystem.SanitizePath(arg.Trim(), s_currentDirectory);
                if (!FileSystem.IsFileOrDirectoryPresent(fileDir))
                {
                    FileSystem.ErrorWriteLine($"Directory/File does not exist: {fileDir}");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                var getAttribute = new AttributesManage(new List<string>(), fileDir);
                getAttribute.GetFileAttributes();
            }
            catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Check if attribute is in command after split.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private static bool IsAttributeFIlePreset(string data, bool isFile)
        {
            if (string.IsNullOrEmpty(data))
            {
                if (isFile)
                    FileSystem.ErrorWriteLine("You need specify the file name!");
                else
                    FileSystem.ErrorWriteLine("You need to add at least one attribute!");
                return false;
            }
            return true;
        }
    }
}
