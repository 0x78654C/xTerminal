using Core;
using System;
using System.Diagnostics;
using System.IO;

namespace xEditor
{
    class Program
    {
        /*Text editor opener*/
        static void Main(string[] args)
        {
            string file = string.Empty;
            string set;
            string dlocation = File.ReadAllText(GlobalVariables.currentLocation);

            if (!File.Exists(GlobalVariables.editorPath))
            {
                File.WriteAllText(GlobalVariables.editorPath, "notepad");
            }
            string cEditor = File.ReadAllText(GlobalVariables.editorPath);
            try
            {
                file = args[0];
                set = args[1];
                if (!file.Contains("set"))
                {

                    if (File.Exists(cEditor))
                    {
                        var process = new Process();
                        process.StartInfo = new ProcessStartInfo(cEditor)
                        {
                            UseShellExecute = false,
                            Arguments = 
                            
                            + @"\" + file

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
                    File.WriteAllText(GlobalVariables.editorPath, @set);
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
