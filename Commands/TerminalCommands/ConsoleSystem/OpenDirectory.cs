using Core;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class OpenDirectory : ITerminalCommand
    {
        public string Name => "odir";


        public void Execute(string arg)
        {
            //reading current location
            string dlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
            if (dlocation == "")
            {
                RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory, GlobalVariables.rootPath);
            }
            FileSystem.OpenCurrentDiretory(dlocation);
        }
    }
}
