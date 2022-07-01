using Core;
using System;
using System.IO;
using System.Linq;
using Json = Core.SystemTools.JsonManage;
using AliasC = Core.SystemTools.AliasC;
using System.Collections.Generic;

namespace Commands.TerminalCommands.ConsoleSystem
{
    public class Alias : ITerminalCommand
    {
        /* Alias commands manager. */

        public string Name => "alias";
        private static string s_aliasFile = GlobalVariables.aliasFile;
        private static List<string> s_paramList = new List<string> { "-del", "-add", "-list", "-clear" };
        private static string s_helpMessage = @"Usage of alias commands:

 -add   :  Creates a alias command with parammeters (alias <commandName>|<parameters>).
           Example: alias -add lz|ls -s (Creates a command lz that will run parameter ls -s)
 -del   :  Deletes a alias command.
           Example: alias -del lz (Deletes lz command and parameters for it.)
 -list  :  List all alias commands.

 -clear :  Clears all alias commands.

 Attention: Alias commands cannot overwrite terminal commands!
";

        public void Execute(string args)
        {
            try
            {
                if (args.Length == 5)
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (args == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                args = args.Replace("alias ", String.Empty);

                if (!s_paramList.Contains(args.Split().First()))
                {
                    Console.WriteLine("This parameter does not exist! Use -h for help.");
                    return;
                }
                if (args.StartsWith("-add"))
                    AddCommand(args, s_aliasFile);

                if (args.StartsWith("-del"))
                    DeleteCommand(args, s_aliasFile);

                if (args.StartsWith("-list"))
                    ListCommands(s_aliasFile);

                if (args.StartsWith("-clear"))
                    ClearAliasCommands(s_aliasFile);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message+ " Check command!\nUse alias -clear if alias file if is corrupted. File will be recreated by adding new command!");
            }
        }

        /// <summary>
        /// Add alias commands and instructions to Json file.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="aliasJsonFile"></param>
        private static void AddCommand(string arg, string aliasJsonFile)
        {
            string commandAlias = arg.SplitByText("-add ", 1);
            if (commandAlias.Contains("|"))
            {
                string commandName = commandAlias.Split('|')[0].Trim();
                if (commandName.Length < 2)
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Command name should be at least 2 characters long!");
                    return;
                }
                string command = commandAlias.Split('|')[1].Trim();
                if (File.Exists(aliasJsonFile) && CheckCommandName(s_aliasFile, commandName))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"{commandName} alias command already exist!");
                    return;
                }
                Json.UpdateJsonFile(aliasJsonFile, new AliasC { CommandName = commandName, Command = command });
                Console.WriteLine($"Alias command '{commandName}' was added!");
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
            string delAliasCommand = arg.SplitByText("-del ", 1);
            if (!File.Exists(aliasJsonFile))
            {
                FileSystem.ErrorWriteLine("Alias file does not exist! You need to first add a command.");
                return;
            }

            if (!CheckCommandName(aliasJsonFile, delAliasCommand))
            {
                FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"{delAliasCommand} does not exist!");
                return;
            }
            Json.DeleteJsonData<AliasC>(s_aliasFile, f => f.Where(t => t.CommandName == delAliasCommand));
            Console.WriteLine($"Alias command '{delAliasCommand}' was deleted!");
        }

        /// <summary>
        /// List stored alias commands.
        /// </summary>
        /// <param name="aliasJsonFile"></param>
        private static void ListCommands(string aliasJsonFile)
        {
            if (!File.Exists(aliasJsonFile))
            {
                FileSystem.ErrorWriteLine("There are no alias commands added!");
                return;
            }
            var aliasCommands = Json.ReadJsonFromFile<AliasC[]>(aliasJsonFile);
            Console.WriteLine("List alias commands:\n");
            foreach (var ac in aliasCommands)
                Console.WriteLine($"{ac.CommandName} | {ac.Command}");
        }

        /// <summary>
        /// Delete json file.
        /// </summary>
        /// <param name="aliasJsonFile"></param>
        private static void ClearAliasCommands(string aliasJsonFile)
        {
            if (File.Exists(aliasJsonFile))
            {
                File.Delete(aliasJsonFile);
                Console.WriteLine("Alias commands ware cleared!");
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
