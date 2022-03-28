using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Shell
{

    class Program
    {
        private static string s_terminalWorkDirectory = GlobalVariables.terminalWorkDirectory;

        // Deletes current directory temp file.
        private static void DeleteCDFIle()
        {

            // Creating the xTerminal directory under current user Appdata/Local.
            if (!Directory.Exists(s_terminalWorkDirectory))
                Directory.CreateDirectory(s_terminalWorkDirectory);

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
            // confgure console
            Console.OutputEncoding = System.Text.Encoding.UTF8;//set utf8 encoding (for support Russian letters)
            DeleteCDFIle();
            var shell = new Shell();
            shell.Run(args);//Running the shell
        }
    }
}
