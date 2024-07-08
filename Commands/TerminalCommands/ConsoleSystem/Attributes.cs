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
        //TODO: fix message and remove unused attributes 
        public string Name => "attr";
        private static string s_currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
        private static string s_helpMessage = @"Usage of locate command:
   
    Parameters:
     -s  : Displays searched files/directories from the current directory and subdirectories that starts with a specific text.
            Example 1: locate -s <text>
            Example 2: locate -s <text> -o <save_to_file>
     -e  : Displays searched files/directories from the current directory and subdirectories that ends with a specific text.
            Example 1: locate -e <text>
            Example 2: locate -e <text> -o <save_to_file>
     -eq : Displays searched files/directories from the current directory and subdirectories that equals a specific text.
            Example 1: locate -eq <text>
            Example 2: locate -eq <text> -o <save_to_file>
  
Command can be canceled with CTRL+X key combination.
";
        public void Execute(string arg)
        {
            try
            {
                arg = arg.Replace($"{Name} ", string.Empty);

                if (arg.StartsWith("-h") && arg.Length == 2)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                // Set attribute
                if(arg.Contains("-s "))
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
                        setAttribute.AttributeSetSingle(attributes, FileSystem.SanitizePath(fileDirName, s_currentDirectory));
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
                        setAttribute.AttributeRemoveSingle(attributes, FileSystem.SanitizePath(fileDirName, s_currentDirectory));
                    }
                    return;
                }
            }
            catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString()); // set to message when release
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
