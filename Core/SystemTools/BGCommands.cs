using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class BGCommands
    {
        private string _xTerminalExe = $"{Application.StartupPath}xTerminal.exe";
        private string _backgroundCommandsPidList = GlobalVariables.bgProcessListFile;

        /// <summary>
        /// Command with/without parameters.
        /// </summary>
        public string Command { get; set; }
        public BGCommands() { }

        /// <summary>
        /// Run background command.
        /// </summary>
        public void ExecuteCommand()
        {
            if (!string.IsNullOrEmpty(Command))
            {
                var commandFirst = Command.Split(' ')[0];
                var pId = ProcessStart.ExecuteWithPidOutput(_xTerminalExe, Command);
                if (!string.IsNullOrEmpty(pId))
                {
                    var param = Command.Replace(commandFirst, "").Trim();
                    var outData = $"Command: {commandFirst} | Param: '{param}' | PID: {pId}";
                    if (File.Exists(_backgroundCommandsPidList))
                    {
                        var isLine = File.ReadAllLines(_backgroundCommandsPidList).Any(line => line.Contains(outData));
                        if(!isLine)
                            File.AppendAllText(_backgroundCommandsPidList, outData+Environment.NewLine);
                    }
                    FileSystem.SuccessWriteLine(outData);
                }
                else
                    FileSystem.ErrorWriteLine($"Command {commandFirst} has not started!");
            }
        }
    }
}
