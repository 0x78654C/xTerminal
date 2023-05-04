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
    >   : Write data to a file.
          Example: echo hello world > path_to_file
    >>  : Append data to a file. 
          Example: echo hello world >> path_to_file
";

        public void Execute(string arg)
        {
            try
            {
                _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
                if (arg.Length == 4)
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }

                // Display help message.
                if(arg == "echo -h")
                {
                    Console.WriteLine(_helpMessage);
                    return;
                }

                // Save data to file.
                if (arg.Contains(" > "))
                {
                    string inputData = string.Empty;
                    if (!arg.StartsWith("echo >"))
                        inputData = arg.MiddleString("echo", ">");
                    string fileOutput = FileSystem.SanitizePath(arg.SplitByText(" > ", 1), _currentLocation);
                    File.WriteAllText(fileOutput, inputData);
                    if (File.Exists(fileOutput))
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Data saved in {fileOutput}");
                }

                // Append data to file.
                if (arg.Contains(">>"))
                {
                    string inputData = string.Empty;
                    if (!arg.StartsWith("echo >"))
                        inputData = arg.MiddleString("echo", ">");
                    string fileOutput = FileSystem.SanitizePath(arg.SplitByText(" >> ", 1), _currentLocation);
                    File.AppendAllText(fileOutput, inputData);
                    if (File.Exists(fileOutput))
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"Data added to {fileOutput}");
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
