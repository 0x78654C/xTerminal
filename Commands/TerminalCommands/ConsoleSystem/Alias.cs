﻿using Core;
using System;
using System.IO;
using System.Linq;
using Json = Core.SystemTools.JsonManage;
using AliasC = Core.SystemTools.AliasC;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("windows")]
    public class Alias : ITerminalCommand
    {
        /* Alias commands manager. */

        public string Name => "alias";
        private static string s_aliasFile = GlobalVariables.aliasFile;
        private static List<string> s_paramList = new List<string> { "-del", "-add", "-list", "-clear", "-update" };
        private static string s_helpMessage = @"Usage of alias commands:

 -add   :  Creates a alias command with parammeters (alias <commandName>*<parameters>).
           Example: alias -add lz*ls -s (Creates a command lz that will run parameter ls -s)
 -del   :  Deletes a alias command.
           Example: alias -del lz (Deletes lz command and parameters for it.)
 -update:  Update a alias command.
           Example: alias -update lz*ls -ct (Updates command lz with new parameters. Works if command already exist!)
 -list  :  List all alias commands.

 -clear :  Clears all alias commands.

 Alias commands can use internal parameters with % character. % will take the input and pass to internal command. 
 Example:
 ~ $ alias -add np * cmd start %
 ~ $ np notepad 
 Attention: Alias commands cannot overwrite terminal commands!
";

        public void Execute(string args)
        {
            try
            {
                if (args == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
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
                    FileSystem.SuccessWriteLine("This parameter does not exist! Use -h for help.");
                    return;
                }

                GlobalVariables.isErrorCommand = false;

                if (args.StartsWith("-add"))
                    AddCommand(args, s_aliasFile);

                if (args.StartsWith("-del"))
                    DeleteCommand(args, s_aliasFile, true);

                if (args.StartsWith("-list"))
                    ListCommands(s_aliasFile);

                if (args.StartsWith("-clear"))
                    ClearAliasCommands(s_aliasFile);

                if (args.StartsWith("-update"))
                    UpdateCommands(args, s_aliasFile);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message + " Check command!\nUse alias -clear if alias file if is corrupted. File will be recreated by adding new command!");
                GlobalVariables.isErrorCommand = true;
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
            if (commandAlias.Contains("*"))
            {
                string commandName = commandAlias.Split('*')[0].Trim();
                if (commandName.Length < 2)
                {
                    FileSystem.ErrorWriteLine("Command name should be at least 2 characters long!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                if (commandName.Length > 14)
                {
                    FileSystem.ErrorWriteLine("Command name should be maxim 14 characters!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                string command = ParseAlias(commandAlias);
                if (File.Exists(aliasJsonFile) && CheckCommandName(s_aliasFile, commandName))
                {
                    FileSystem.ErrorWriteLine($"{commandName} alias command already exist!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                Json.UpdateJsonFile(aliasJsonFile, new AliasC { CommandName = commandName, Command = command });
                FileSystem.SuccessWriteLine($"Alias command '{commandName}' was added!");
            }
            else
            {
                FileSystem.ErrorWriteLine("Name should be separated from command with * , example: name*command to use");
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Delete alias commands from Json file.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="aliasJsonFile"></param>
        /// <param name="updateFlag"></param>
        private static void DeleteCommand(string arg, string aliasJsonFile, bool updateFlag)
        {
            string delAliasCommand;
            if (updateFlag)
                delAliasCommand = arg.SplitByText("-del ", 1);
            else
                delAliasCommand = arg;

            if (!File.Exists(aliasJsonFile) && updateFlag)
            {
                FileSystem.ErrorWriteLine("Alias file does not exist! You need to first add a command.");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            if (!CheckCommandName(aliasJsonFile, delAliasCommand) && updateFlag)
            {
                FileSystem.ErrorWriteLine($"{delAliasCommand} does not exist!");
                GlobalVariables.isErrorCommand = true;
                return;
            }
            Json.DeleteJsonData<AliasC>(s_aliasFile, f => f.Where(t => t.CommandName == delAliasCommand));
            if (updateFlag)
                FileSystem.SuccessWriteLine($"Alias command '{delAliasCommand}' was deleted!");
        }

        /// <summary>
        /// Update alias commands from Json file.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="aliasJsonFile"></param>
        private static void UpdateCommands(string arg, string aliasJsonFile)
        {
            string updateAliasCommand = arg.SplitByText("-update ", 1);
            if (updateAliasCommand.Contains("*"))
            {
                if (!File.Exists(aliasJsonFile))
                {
                    FileSystem.ErrorWriteLine("Alias file does not exist! You need to first add a command.");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                string commandName = updateAliasCommand.Split().First();
                var commandLeng = commandName.Length;
                string command = updateAliasCommand.Substring(commandLeng + 1).Trim();
                if (!CheckCommandName(aliasJsonFile, commandName))
                {
                    FileSystem.SuccessWriteLine($"{commandName} does not exist!");
                    return;
                }
                DeleteCommand(commandName, aliasJsonFile, false);
                Json.UpdateJsonFile(aliasJsonFile, new AliasC { CommandName = commandName, Command = command.Substring(1).Trim() });
                FileSystem.SuccessWriteLine($"Alias command '{commandName}' was updated!");
            }
            else
            {
                GlobalVariables.isErrorCommand = true;
                FileSystem.ErrorWriteLine("Name should be separated from command with * , example: name*command to use");
            }
        }

        /// <summary>
        /// Parse alias command name from command to run.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        private static string ParseAlias(string alias)
        {
            var command = alias.Split('*')[0];
            return alias.SplitByText($"{command}*", 1);
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
                GlobalVariables.isErrorCommand = true;
                return;
            }
            var aliasCommands = Json.ReadJsonFromFile<AliasC[]>(aliasJsonFile);
            Console.WriteLine("List alias commands:\n");
            foreach (var ac in aliasCommands)
            {
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput += $"{ac.CommandName} | {ac.Command}";
                else
                    Console.WriteLine($"{ac.CommandName} | {ac.Command}");
            }
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
                FileSystem.SuccessWriteLine("Alias commands ware cleared!");
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
