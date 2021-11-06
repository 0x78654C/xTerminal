namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     * Start new terminal window.
     */

    class NewTWindow : ITerminalCommand
    {
        public string Name => "nt";
        public void Execute(string arg)
        {
            if (arg.ContainsText("-u"))
            {
                Core.SystemTools.ProcessStart.ProcessExecute("xTerminal.exe", "", true, true);
                return;
            }
            Core.SystemTools.ProcessStart.ProcessExecute("xTerminal.exe", "", true, false);
        }
    }
}
