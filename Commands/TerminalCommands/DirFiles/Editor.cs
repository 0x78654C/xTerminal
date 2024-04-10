using Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class Editor : ITerminalCommand
    {
        public string Name => "edit";
        private static string s_helpMessage = @"Usage of edit command:
    -set     : Sets the text editor you want to use. (Default is notepad)
               Example: edit -set <path_to_editor_binary>
    -current : Displays the current used editor.
    -h       : Displays this message.
";
        public void Execute(string arg)
        {
            string file = string.Empty;
            string set= string.Empty;
            string dlocation = File.ReadAllText(GlobalVariables.currentDirectory);
            string cEditor = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentEitor);


            // Set default editor if none is set.
            if (cEditor == "")
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentEitor, "notepad");

            try
            {
                if(arg == "edit -h".Trim())
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                if (arg.Contains("-current"))
                {
                    string currentEditor = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentEitor);
                    FileSystem.SuccessWriteLine("Your current editor is: " + currentEditor);
                    return;
                }
                if (!arg.Contains("-set"))
                {
                    int argLenght = arg.Length - 5;
                    if (GlobalVariables.isPipeCommand)
                        file = GlobalVariables.pipeCmdOutput.Trim();
                    else
                        file = arg.Substring(5, argLenght);
                    file = FileSystem.SanitizePath(file, dlocation);
                    ProcessCall(file, File.Exists(cEditor) ? cEditor : "notepad");
                    return;
                }
                set = arg.Replace("edit -set ", "");
                if (File.Exists(set))
                {
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentEitor, set);
                    FileSystem.SuccessWriteLine("Your New editor is: " + set);
                    return;
                }
                FileSystem.ErrorWriteLine($"File {set} does not exist!");
                return;
            }
            catch
            {
                file = FileSystem.SanitizePath(file, dlocation);
                if (string.IsNullOrEmpty(file))
                {
                    FileSystem.ErrorWriteLine("You must type name of the file to be edited!");
                    return;
                }
                ProcessCall(file, File.Exists(cEditor) ? cEditor : "notepad");
                return;
            }
        }

        /// <summary>
        /// Edit file with predifined editor(notepad) or custom one.
        /// </summary>
        /// <param name="file">File to be edited.</param>
        /// <param name="currentEditor">Editor path.</param>
        private void ProcessCall(string file, string currentEditor)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(currentEditor)
            {
                UseShellExecute = false,
                Arguments = file
            };
            process.Start();
        }
    }
}
