using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Commands.TerminalCommands.DirFiles
{
    public class Editor : ITerminalCommand
    {
        public string Name => "edit";

        public void Execute(string arg)
        {

            string file = string.Empty;
            string set;

            string dlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);


            string cEditor = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentEitor);
            if (cEditor == "")
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentEitor, "notepad");
            }

            try
            {
                file = arg.Split(' ')[1];
                set = arg.Split(' ')[1];
                if (!file.Contains("set"))
                {

                    if (File.Exists(cEditor))
                    {
                        var process = new Process();
                        process.StartInfo = new ProcessStartInfo(cEditor)
                        {
                            UseShellExecute = false,
                            Arguments = dlocation + @"\" + file

                        };

                        process.Start();
                        process.WaitForExit();
                    }
                    else
                    {

                        var process = new Process();
                        process.StartInfo = new ProcessStartInfo("notepad")
                        {
                            UseShellExecute = false,
                            Arguments = dlocation + @"\" + file

                        };

                        process.Start();
                        process.WaitForExit();
                    }
                }
                else
                {
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentEitor, @set);
                    Console.WriteLine("Your New editor is: " + @set);
                }

            }
            catch
            {

                if (File.Exists(cEditor))
                {
                    if (string.IsNullOrEmpty(file))
                    {
                        Console.WriteLine("You must type the file name for edit!");
                    }
                    else
                    {
                        var process = new Process();
                        process.StartInfo = new ProcessStartInfo(cEditor)
                        {
                            UseShellExecute = false,
                            Arguments = dlocation + @"\" + file

                        };

                        process.Start();
                        process.WaitForExit();
                    }
                }
                else
                {
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo("notepad")
                    {
                        UseShellExecute = false,
                        Arguments = dlocation + @"\" + file

                    };

                    process.Start();
                    process.WaitForExit();
                }
            }
        }
    }
}
