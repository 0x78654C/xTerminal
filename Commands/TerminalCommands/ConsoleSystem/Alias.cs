using Core;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Json = Core.SystemTools.JsonManage;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class Alias : ITerminalCommand
    {

        public string Name => "alias";
        private static string s_aliasFile = GlobalVariables.aliasFile;
        public void Execute(string args)
        {
            try
            {

                args = args.Replace("alias ", String.Empty);
                if (args.StartsWith("add"))
                {
                    string commandAlias = args.SplitByText("add ", 1);
                    if (commandAlias.Contains("|"))
                    {
                        string commandName = commandAlias.Split('|')[0];
                        string command = commandAlias.Split('|')[1];
                        AddCommand(commandName, command);
                        Console.WriteLine($"Alias command {commandName} was added!");
                    }
                    else
                        FileSystem.ErrorWriteLine("Name should be separated from command with | , example: name|command to use");
                }
            }catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
            }
        }

        private static void AddCommand(string commandName, string command)
        {
           if (!File.Exists(s_aliasFile))
                Json.CreateJsonFile(s_aliasFile, new { CommandName= commandName, Command= command});
           Json.UpdateJsonFile(s_aliasFile, new AliasC { CommandName = commandName, Command = command });
        }

        private static void DeleteCommand(string commandName)
        {
            if(!File.Exists(s_aliasFile))
            {
                FileSystem.ErrorWriteLine("Alias file dose not exist!");
                return;
            }
            Json.DeleteJsonData<AliasC>(s_aliasFile, f => f.Where(t => t.CommandName == commandName));
        }
    }
    class AliasC
    {
        public string CommandName { get; set; }
        public string Command { get; set; }
    }
}
