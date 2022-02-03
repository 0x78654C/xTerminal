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
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler(CtrlType sig);
        private static EventHandler _handler;

        // Enumarate the API events.
        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

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

        // Delete the current directory file on any of the events are triggered
        private static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_LOGOFF_EVENT:
                    DeleteCDFIle();
                    Environment.Exit(0);
                    return false;
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                    DeleteCDFIle();
                    Environment.Exit(0);
                    return false;
                case CtrlType.CTRL_BREAK_EVENT:
                    DeleteCDFIle();
                    Environment.Exit(0);
                    return false;
                case CtrlType.CTRL_CLOSE_EVENT:
                    DeleteCDFIle();
                    Environment.Exit(0);
                    return false;
                default:
                    return false;
            }
        }

        static void Main(string[] args)
        {
            // confgure console
            Console.OutputEncoding = System.Text.Encoding.UTF8;//set utf8 encoding (for support Russian letters)
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true); // New event for listening on close console event.
            var shell = new Shell();
            shell.Run(args);//Running the shell
        }
    }
}
