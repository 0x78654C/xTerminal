using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Core;
using System.Text.RegularExpressions;

namespace Commands.TerminalCommands.Roslyn
{
    public class AddonManagement : ITerminalCommand
    {
        public string Name => "!";

        private string _codeToRun;
        private string _namesapce;
        private string _className;
        private bool _commandCheck=false;
        private string _currentLocation = string.Empty;
        private string _addonDir = string.Empty;
        private string _helpMessage = @"
 Usage: ! <command_name>
 Can be used with the follwing parameters:

   -h     :  Displays help message.
   -p     :  Uses command with parameters.
                Ex.: ! <command_name> -p <parameters>
   -add   :  Adds new code from a file and stores in Add-ons directory under xTerminal.exe
             current directory with a command name.
                Ex.: ! -add <file_name_with_code> <command_name>|<command_description>
   -list  :  Display the list of the saved add-ons with description.
";


        public void Execute(string args)
        {
            _currentLocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);
            _addonDir =  GlobalVariables.addonDirectory;
            string param = string.Empty;
            string command = args.Split(' ').ParameterAfter("!");
            if (args.ContainsText("-p"))
            {
                param = args.Replace($"! {command} -p ", "");
                CompileAndRun(_addonDir,command, param);
                return;
            } else if (command.StartsWith("-add"))
            {
                SaveAddon(args,_addonDir);
                return;
            }
            else if (command.StartsWith("-h"))
            {
                Console.WriteLine(_helpMessage);
                return;
            }
            else if (command.StartsWith("-list"))
            {
                Console.WriteLine(ListAddons(_addonDir));
                return;
            }
            CompileAndRun(_addonDir, command, param);
        }

        private string ListAddons(string addonDir)
        {
            string fileName;
            string description;
            string outList="List of Add-ons commands:\n\n";
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
        private void SaveAddon(string argument,string addonDir)
        {
            try
            {
                if (!Directory.Exists(addonDir))
                {
                    FileSystem.ErrorWriteLine($"Directory {addonDir} does not exist!");
                    return;
                }

                string file = FileSystem.SanitizePath(argument.Replace($"! -add ", "").Split(' ')[0], _currentLocation);

                if (!File.Exists(file))
                {
                    FileSystem.ErrorWriteLine($"File {file} does not exist!");
                    return;
                }

                string argParse = argument.Replace($"! -add ", "").Split(' ')[1];
                string command = argParse.Split('|')[0];
                int countSpace = Regex.Matches(argument, " ").Count;
                string description = argument.Split('|')[1];

                using (StreamReader stringReader = new StreamReader(file))
                {
                    string fileContent = $"//D:{description}"+Environment.NewLine;
                    fileContent += stringReader.ReadToEnd();
                    File.WriteAllText(addonDir + $"\\{command}.x", fileContent);
                    Console.WriteLine($"Add '{command}' added!");
                }
            }catch(Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message +" Check Command!");
            }
        }
        private void CompileAndRun(string addonDir, string command, string param)
        {
            try
            {
                ParseCode(addonDir,command);
                if (_commandCheck)
                {
                    Console.WriteLine($"The following add-on does not exist: {command}");
                    _commandCheck = false;
                    return;
                }
                Assembly assembly = null;
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(_codeToRun);
                string assemblyName = Path.GetRandomFileName();
                MetadataReference[] references = new MetadataReference[]
                {
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
                };

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

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
                            Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                        }
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        assembly = Assembly.Load(ms.ToArray());
                    }

                    ms.Close();
                }
                Type type = assembly.GetType($"{_namesapce}.{_className}");
                object obj = Activator.CreateInstance(type);
                type.InvokeMember("Main",
                    BindingFlags.Default | BindingFlags.InvokeMethod,
                    null,
                    obj,
                    new object[] { param });
                _className = "";
                _namesapce = "";
                _codeToRun = "";
                obj = null;
                type = null;
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
                if(!files.Contains(commandFile+".x"))
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
                        string[] fileLines = File.ReadAllLines(fileName);

                        foreach (var line in fileLines)
                        {
                            if (line.ContainsText("namespace"))
                            {
                                _namesapce = line.Split(' ').ParameterAfter("namespace");
                            }

                            if (line.ContainsText("public class"))
                            {
                                _className = line.Split(' ').ParameterAfter("class");
                            }
                        }
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
