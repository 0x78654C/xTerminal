using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using Core;

namespace xEditor
{
    class Program
    {
        /*Text editor opener*/
        static void Main(string[] args)
        {
            string file=string.Empty;
            string set = string.Empty;
            string dlocation = File.ReadAllText(FileSystem.CurrentLocation);
            
            if (!File.Exists(FileSystem.EditorPath))
            {
                File.WriteAllText(FileSystem.EditorPath, "notepad");
            }
            string cEditor = File.ReadAllText(FileSystem.EditorPath);
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
                    File.WriteAllText(FileSystem.EditorPath, @set);
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
