using Core;
using System;
using System.IO;
using System.Linq;
using Json = Core.SystemTools.JsonManage;
using AliasC = Core.SystemTools.AliasC;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class Alias : ITerminalCommand
    {
        /* Alias commands manager. */

        public string Name => "alias";
        private static string s_aliasFile = GlobalVariables.aliasFile;
        public void Execute(string args)
        {
            try
            {
                args = args.Replace("alias ", String.Empty);
                if (args.StartsWith("add"))
                    AddCommand(args, s_aliasFile);

                if (args.StartsWith("del"))
                    DeleteCommand(args, s_aliasFile);

                if (args.StartsWith("list"))
                    ListCommands(s_aliasFile);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Add alias commands and instructions to Json file.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="aliasJsonFile"></param>
        private static void AddCommand(string arg, string aliasJsonFile)
        {
            string commandAlias = arg.SplitByText("add ", 1);
            if (commandAlias.Contains("|"))
            {
                string commandName = commandAlias.Split('|')[0].Trim();
                string command = commandAlias.Split('|')[1].Trim();
                if (CheckCommandName(s_aliasFile, commandName))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"{commandName} alias command already exist!");
                    return;
                }
                Json.UpdateJsonFile(aliasJsonFile, new AliasC { CommandName = commandName, Command = command });
                Console.WriteLine($"Alias command {commandName} was added!");
            }
            else
                FileSystem.ErrorWriteLine("Name should be separated from command with | , example: name|command to use");
        }

        /// <summary>
        /// Delete alias commands from Json file.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="aliasJsonFile"></param>
        private static void DeleteCommand(string arg, string aliasJsonFile)
        {
            string delAliasCommand = arg.SplitByText("del ", 1);
            if (!File.Exists(aliasJsonFile))
            {
                FileSystem.ErrorWriteLine("Alias file does not exist!");
                return;
            }

            if (!CheckCommandName(aliasJsonFile, delAliasCommand))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"{delAliasCommand} does not exist!");
                return;
            }
            Json.DeleteJsonData<AliasC>(s_aliasFile, f => f.Where(t => t.CommandName == delAliasCommand));
            Console.WriteLine($"{delAliasCommand} alias command was deleted!");
        }

        /// <summary>
        /// List stored alias commands.
        /// </summary>
        /// <param name="aliasJsonFile"></param>
        private static void ListCommands(string aliasJsonFile)
        {
            if (!File.Exists(aliasJsonFile))
            {
                FileSystem.ErrorWriteLine("Alias file dose not exist!");
                return;
            }
            var aliasCommands = Json.ReadJsonFromFile<AliasC[]>(aliasJsonFile);
            Console.WriteLine("List alias commands:\n");
            foreach(var ac in aliasCommands)
            {
                Console.WriteLine($"{ac.CommandName} | {ac.Command}");
            }
        }

        /// <summary>
        /// Check if alias command exist.
        /// </summary>
        /// <param name="aliasJsonFile"></param>
        /// <param name="commandName"></param>
        /// <returns></returns>
        private static bool CheckCommandName(string aliasJsonFile, string commandName)
        {
            var item = Json.ReadJsonFromFile<AliasC[]>(aliasJsonFile);
            return (item.Any(t => t.CommandName == commandName));
        }
    }
}
