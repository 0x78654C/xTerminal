using System;
using System.IO;
using System.Runtime.Versioning;
using Core;

namespace Commands.TerminalCommands.DirFiles
{
    /* Implementation of echo command for wirte/append data to a file. */
    [SupportedOSPlatform("Windows")]
    public class Echo : ITerminalCommand
    {
        public string Name => "echo";
        private string _currentLocation;
        private string _helpMessage = @"Usage of echo command:
    echo <text> :  Displays in console the <text> data.
    >   : Write data to a file.
          Example: echo hello world > path_to_file
    >>  : Append data to a file. 
          Example: echo hello world >> path_to_file
    -con: Concatenate files data to a single file.
          Example: echo -con file1;file2 -o file3
    -e  : Displays text in console including Unicode escape sequances.
          Example: echo -e <text>  
";

        public void Execute(string arg)
        {
            try
            {
                _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
                if (arg == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                // Display help message.
                if (arg == "echo -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }
                int argsLenght = arg.Length - 4;

                // Save data to file.
                if (arg.Contains(" > "))
                {
                    string inputData = string.Empty;
                    string fileOutput = string.Empty;
                    if (GlobalVariables.isPipeCommand)
                    {
                        inputData = GlobalVariables.pipeCmdOutput;
                        fileOutput = FileSystem.SanitizePath(arg.SplitByText(" > ", 1).Trim(), _currentLocation);
                    }
                    else
                    {
                        if (!arg.StartsWith("echo >"))
                            inputData = arg.MiddleString("echo", ">");
                        fileOutput = FileSystem.SanitizePath(arg.SplitByText(" > ", 1).Trim(), _currentLocation);
                    }
                    File.WriteAllText(fileOutput, inputData);
                    if (File.Exists(fileOutput))
                        FileSystem.SuccessWriteLine($"Data saved in {fileOutput}");
                    return;
                }

                // Append data to file.
                if (arg.Contains(">>"))
                {
                    string inputData = string.Empty;
                    string fileOutput = string.Empty;
                    if (GlobalVariables.isPipeCommand)
                    {
                        inputData = GlobalVariables.pipeCmdOutput;
                        fileOutput = FileSystem.SanitizePath(arg.SplitByText(" >> ", 1).Trim(), _currentLocation);
                    }
                    else
                    {
                        if (!arg.StartsWith("echo >"))
                            inputData = arg.MiddleString("echo", ">");
                        fileOutput = FileSystem.SanitizePath(arg.SplitByText(" >> ", 1).Trim(), _currentLocation);
                    }
                    File.AppendAllText(fileOutput, inputData);
                    if (File.Exists(fileOutput))
                        FileSystem.SuccessWriteLine($"Data added to {fileOutput}");
                    return;
                }

                // Concatenate files
                if (arg.Contains("-con"))
                {
                    var inputData = arg.MiddleString("-con", "-o").Split(';');
                    var outputData = string.Empty;
                    if (!arg.Contains(";"))
                    {
                        FileSystem.ErrorWriteLine("You need to provide minim two files for concatenate!. Use -h for more information");
                        GlobalVariables.isErrorCommand = true;
                        return;
                    }

                    foreach (var item in inputData)
                    {
                        var pathItem = FileSystem.SanitizePath(item, _currentLocation);
                        if (File.Exists(pathItem))
                        {
                            var readData = File.ReadAllText(pathItem);
                            outputData += readData;
                        }
                    }

                    var path = arg.SplitByText("-o", 1).Trim();
                    if (string.IsNullOrEmpty(path))
                    {
                        FileSystem.ErrorWriteLine("You need to provide an output file!. Use -h for more information");
                        GlobalVariables.isErrorCommand = true;
                        return;
                    }
                    var store = FileSystem.SaveFileOutput(path, _currentLocation, outputData);
                    FileSystem.SuccessWriteLine(store);
                    return;
                }

                if (arg.Contains("-e"))
                {   
                    var argE = arg.SplitByText("-e", 1).Trim();
                    var procesedInput = GlobalVariables.isPipeCommand? FileSystem.ConvertUnicodeEscapes(GlobalVariables.pipeCmdOutput) : FileSystem.ConvertUnicodeEscapes(argE);
                    FileSystem.SuccessWriteLine(procesedInput);
                    return;
                }
                var args = arg.Substring(4, argsLenght).Trim();
                FileSystem.SuccessWriteLine(args);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
