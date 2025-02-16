﻿using Core;
using Core.SystemTools;
using System;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Shortcut : ITerminalCommand
    {
        public string Name => "ln";
        private static string s_helpMessage = @"Usage of ln command parameters:
    ln <path_file_folder> : Create shortcut of a specific file/directory on Desktop.
    ln <path_file_folder> -o <path_location_shortcut> : Create shortcut in a specific location.
";
        public void Execute(string arg)
        {
            try
            {
                if (arg == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine("Use -h for more information!");
                    return;
                }

                arg = arg.Substring(2);

                // Display help message.
                if (arg.Trim() == "-h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                var shortcutMaker =  new ShortcutMaker();

                // Save shortcut to specific directory.
                if(arg.Contains("-o "))
                {
                    var fileShort = "";
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        fileShort = GlobalVariables.pipeCmdOutput.Trim();
                    else 
                        arg.SplitByText(" -o ", 0).Trim();
                    var pathShort = arg.SplitByText(" -o ", 1).Trim();
                    shortcutMaker.PathShortcut = pathShort;
                    shortcutMaker.Path = fileShort;
                    shortcutMaker.SaveDesktop = false;
                    shortcutMaker.CreateShortcut();
                    return;
                }
                var fileShortDesk = "";
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    fileShortDesk = GlobalVariables.pipeCmdOutput.Trim();
                else
                    fileShortDesk = arg.Trim();
                shortcutMaker.Path =  fileShortDesk;
                shortcutMaker.CreateShortcut();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
