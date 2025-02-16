using Core;
using System;
using System.IO;
using System.Runtime.Versioning;
using Core.SystemTools;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class JsonPrettifier : ITerminalCommand
    {
        public string Name => "pjson";
        private static string s_helpMessage = @"Usage of pjson command:
  pjson -h                             : Display this message. 
  pjson <file_path>                    : Will prettify the JSON data and stores back in file.
  pjson <file_path> -o <new_file_path> : Stores the prettified JSON in new file.   
";
        public void Execute(string arg)
        {
            if (arg == $"{Name} -h" && !GlobalVariables.isPipeCommand)
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            if (arg == Name && !GlobalVariables.isPipeCommand)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }

            arg = arg.Replace($"{Name}", "").Trim();
            var currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
            var param = arg.Split(' ');
            // Store to file param
            if (param.ContainsParameter("-o"))
            {
                var jFile = arg.SplitByText("-o")[0].Trim();
                jFile = FileSystem.SanitizePath(jFile, currentDirectory);
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    jFile = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), currentDirectory);
                var newFile = arg.SplitByText("-o")[1].Trim();
                newFile = FileSystem.SanitizePath(newFile, currentDirectory);

                if (!File.Exists(jFile))
                {
                    FileSystem.ErrorWriteLine($"Original file not found: {jFile}");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                if (File.Exists(newFile))
                {
                    Console.WriteLine($"File already exist: {newFile}.");
                    Console.Write($"Do you want to overwrite it? YES (Y), NO (N): ");
                    var userImputKey = Console.ReadKey();

                    switch (userImputKey.KeyChar.ToString().ToLower())
                    {
                        case "y":
                            var readJsonFileOriginal = File.ReadAllText(jFile);
                            var jsonPrettyOriginal = JsonManage.JsonPrettifier(readJsonFileOriginal);
                            Console.Write("\n");
                            File.WriteAllText(newFile, jsonPrettyOriginal);
                            if (newFile != jFile)
                                FileSystem.SuccessWriteLine($"Current JSON file prettified: {newFile}");
                            else
                                FileSystem.SuccessWriteLine($"Original JSON file prettified: {newFile}");
                            break;
                        default:
                            Console.Write("\n");
                            break;

                    }
                }
                else
                {
                    var readJsonFileOriginal = File.ReadAllText(jFile);
                    var jsonPrettyOriginal = JsonManage.JsonPrettifier(readJsonFileOriginal);
                    File.WriteAllText(newFile, jsonPrettyOriginal);
                    FileSystem.SuccessWriteLine($"Saved new JSON file prettified: {newFile}");
                }
                return;
            }

            var jsonFile = FileSystem.SanitizePath(arg, currentDirectory).Trim();
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                jsonFile = FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), currentDirectory);

            if (!File.Exists(jsonFile))
            {
                FileSystem.ErrorWriteLine($"File not found: {jsonFile}");
                GlobalVariables.isErrorCommand = true;
                return;
            }

            var readJsonFile = File.ReadAllText(jsonFile);
            var jsonPretty = JsonManage.JsonPrettifier(readJsonFile);


            if (File.Exists(jsonFile))
            {
                File.WriteAllText(jsonFile, jsonPretty);
                FileSystem.SuccessWriteLine($"Original JSON file prettified: {jsonFile}");
            }
        }
    }
}
