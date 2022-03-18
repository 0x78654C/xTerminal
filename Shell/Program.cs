using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Shell
{

    class Program
    {
        // Deletes current directory temp file.
        private static void DeleteCDFIle()
        {
            var getFiles = Directory.GetFiles(GlobalVariables.terminalWorkDirectory);
            var listFilesID = new List<string>();
            var listProcessID = new List<string>();
            foreach (var file in getFiles)
            {
                if (file.EndsWith("cDir.t"))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    var fileCDir = fileInfo.Name.Replace("cDir.t", "");
                    listFilesID.Add(fileCDir);
                }
            }
            foreach (var process in Process.GetProcessesByName("xTerminal"))
            {
                listProcessID.Add(process.Id.ToString());
            }

            var finalListID = listFilesID.Except(listProcessID).ToList();

            foreach (var file in getFiles)
            {
                foreach (var item in finalListID)
                {
                    if (file.EndsWith("cDir.t") && file.Contains(item))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        File.Delete(fileInfo.FullName);
                    }
                }
            }

            if (File.Exists(GlobalVariables.currentDirectory))
                File.Delete(GlobalVariables.currentDirectory);
        }

        static void Main(string[] args)
        {
            /* Eliminates the need to import an unmanaged library and stops IOException error
               when closing xTerminal from the title bar. */
            AppDomain.CurrentDomain.ProcessExit += (s, e) => DeleteCDFIle();

            // confgure console
            Console.OutputEncoding = System.Text.Encoding.UTF8;//set utf8 encoding (for support Russian letters)
            var shell = new Shell();
            shell.Run(args);//Running the shell
        }
    }
}
