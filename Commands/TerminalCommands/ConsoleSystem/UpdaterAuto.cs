using Core;
using Core.Updater;
using System;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class UpdaterAuto : ITerminalCommand
    {
        public string Name => "xup";
        private string _helpMessage = @"Usage of xup command:

    xup -e : Enables checking for updates on startup.
    xup -d : Disables checking for updates on startup.
    xup -s : Check if auto-update is enabled or disabled.
    xup -c : Checks for updates and displays the result.
    xup -h : Displays this help message.
";

        public void Execute(string arg)
        {
            try
            {
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }
                arg = arg.Replace($"{Name} ", "").Trim();

                // Enable
                if (arg == "-e")
                {
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regAutoUpdate, "True");
                    FileSystem.SuccessWriteLine("Auto-update check on startup has been enabled.");
                }

                // Disable
                if (arg == "-d")
                {
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regAutoUpdate, "False");
                    FileSystem.SuccessWriteLine("Auto-update check on startup has been disabled.");
                }

                // Status
                if (arg == "-s")
                {
                    var status = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regAutoUpdate);
                    FileSystem.SuccessWriteLine($"Auto-update check on startup is: {(status == "True" ? "enabled" : "disabled")}.");
                }

                if (arg == "-c")
                {
                    var gitApi = new GitHubAPI();
                    gitApi.CheckUpdate(true);
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
