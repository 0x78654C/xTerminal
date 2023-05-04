using Core;
using Core.SystemTools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using GetRef = Core.SystemTools.Roslyn;

namespace Commands.TerminalCommands.Roslyn
{
    [SupportedOSPlatform("Windows")]
    public class AddonManagement : ITerminalCommand
    {

        /*
        Add-on management class for running C# code with Roslyn 
        Link: https://github.com/dotnet/roslyn
         */

        public string Name => "!";

        private string _codeToRun;
        private string[] _commandLineArgs;
        private bool _commandCheck = false;
        private string _currentLocation = string.Empty;
        private string _addonDir = string.Empty;
        private string _helpMessage = @"
 Usage: ! <command_name>
 Can be used with the following parameters:

   -h     :  Displays help message.
   -p     :  Uses command with parameters.
                Example: ! <command_name> -p <parameters>
   -add   :  Adds new code from a file and stores in Add-ons directory under xTerminal.exe
             current directory with a command name.
                Example: ! -add <file_name_with_code> -c <command_name>|<command_description>
   -del   :  Deletes an Add-on.
                Example: ! -del <command_name>
   -list  :  Displays the list of the saved add-ons with description.
";


        public void Execute(string args)
        {
            _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
            _addonDir = GlobalVariables.addonDirectory;
            if (args.Length == 1)
            {
                Console.WriteLine($"Use -h param for {Name} command usage!");
                return;
            }
            string param = string.Empty;
            string command = args.Split(' ').ParameterAfter("!");
            if (args.ContainsText("-p"))
            {
                param = args.Replace($"! {command} -p ", "");
                CompileAndRun(_addonDir, command, param);
                return;
            }
            else if (command.StartsWith("-add"))
            {
                SaveAddon(args, _addonDir);
                return;
            }
            else if (args == $"{Name} -h")
            {
                Console.WriteLine(_helpMessage);
                return;
            }
            else if (command.StartsWith("-list"))
            {
                Console.WriteLine(ListAddons(_addonDir));
                return;
            }
            else if (command.StartsWith("-del"))
            {
                string fileName = args.Split(' ').ParameterAfter("-del");
                DeleteAddon(_addonDir, fileName);
                return;
            }
            CompileAndRun(_addonDir, command, param);
        }

        private string ListAddons(string addonDir)
        {
            string fileName;
            string description;
            string outList = "List of Add-on commands:\n\n";
            if (!Directory.Exists(addonDir))
            {
                FileSystem.ErrorWriteLine($"Directory {addonDir} does not exist!");
            }
            else
            {
                var files = Directory.GetFiles(addonDir);
                foreach (var file in files)
                {
                    if (file.Contains(".x"))
                    {
                        var fileInfo = new FileInfo(file);
                        fileName = fileInfo.Name.Replace(".x", "").PadRight(15, ' ');
                        string line = File.ReadAllLines(file).First();
                        description = line.Replace("//D:", "") + "\n";
                        outList += $"{fileName}  :  {description}";
                    }
                }
            }

            return outList;
        }

        private void DeleteAddon(string addonDir, string fileName)
        {
            try
            {
                if (!Directory.Exists(addonDir))
                {
                    FileSystem.ErrorWriteLine($"Directory {addonDir} does not exist!");
                    return;
                }
                var files = Directory.GetFiles(addonDir);
                foreach (var file in files)
                {
                    if (Path.GetFileName(file) == fileName + ".x")
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted Add-on: {fileName}");
                    }
                }

            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message + " Check Command!");
            }
        }
        private void SaveAddon(string argument, string addonDir)
        {
            try
            {
                if (!Directory.Exists(addonDir))
                {
                    FileSystem.ErrorWriteLine($"Directory {addonDir} does not exist!");
                    return;
                }
                string dirFirst = argument.SplitByText("-add ", 1);
                string dir = dirFirst.SplitByText(" -c ", 0);
                string file = FileSystem.SanitizePath(dir, _currentLocation);

                if (!File.Exists(file))
                {
                    FileSystem.ErrorWriteLine($"File {file} does not exist!");
                    return;
                }

                string argParse = dirFirst.SplitByText(" -c ", 1);
                string command = argParse.Split('|')[0].Trim();
                if(command.Length < 2)
                {
                    FileSystem.ErrorWriteLine("Command name should be at least 2 characters long!");
                    return;
                }
                if(command.Length > 14)
                {
                    FileSystem.ErrorWriteLine($"Command name should be maxim 14 characters!");
                    return;
                }
                int countSpace = Regex.Matches(argument, " ").Count;
                string description = argument.Split('|')[1].Trim();

                using (StreamReader stringReader = new StreamReader(file))
                {
                    string fileContent = $"//D:{description}" + Environment.NewLine;
                    fileContent += stringReader.ReadToEnd();
                    File.WriteAllText(addonDir + $"\\{command}.x", fileContent);
                    Console.WriteLine($"Add-on '{command}' added!");
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message + " Check Command!");
            }
        }
        private void CompileAndRun(string addonDir, string command, string param)
        {
            try
            {
                SplitArguments splitArguments = new SplitArguments(param);
                _commandLineArgs = splitArguments.CommandLineToArgs() ?? Array.Empty<string>();
                ParseCode(addonDir, command);
                if (_commandCheck)
                {
                    Console.WriteLine($"The following Add-on does not exist: {command}");
                    _commandCheck = false;
                    return;
                }
                Assembly assembly = null;
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(_codeToRun);
                string assemblyName = Path.GetRandomFileName();
                var references = GetRef.References();

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach (Diagnostic diagnostic in failures)
                        {
                            var lineError = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
                            Console.Error.WriteLine("{0}: {1} -> line {2}", diagnostic.Id, diagnostic.GetMessage(), lineError);
                        }
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        assembly = Assembly.Load(ms.ToArray());
                    }

                    ms.Close();
                }
                MethodInfo myMethod = assembly.EntryPoint;
                myMethod.Invoke(null, new object[] { _commandLineArgs });
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        private void ParseCode(string addonDir, string commandFile)
        {
            try
            {

                if (!Directory.Exists(addonDir))
                {
                    FileSystem.ErrorWriteLine($"Directory {addonDir} does not exist!");
                    return;
                }

                var dirFiles = Directory.GetFiles(addonDir);
                string files = string.Join("|", dirFiles);
                if (!files.Contains(commandFile + ".x"))
                {
                    _commandCheck = true;
                    return;
                }
                foreach (var file in dirFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Name.Contains(commandFile + ".x"))
                    {

                        string fileName = FileSystem.SanitizePath(fileInfo.FullName, _currentLocation);
                        if (!File.Exists(fileName))
                        {
                            FileSystem.ErrorWriteLine($"File {fileName} does not exist!");
                            return;
                        }
                        _codeToRun = File.ReadAllText(fileName);
                    }
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

    }
}
