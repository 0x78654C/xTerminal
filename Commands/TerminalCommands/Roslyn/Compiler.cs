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
using GetRef = Core.SystemTools.Roslyn;

namespace Commands.TerminalCommands.Roslyn
{
    [SupportedOSPlatform("Windows")]
    public class Compiler : ITerminalCommand
    {
        /*
         Compiles C# in memory using Roslyn 
         */
        public string Name => "ccs";
        private string _codeToRun;
        private string _currentLocation = string.Empty;
        private string[] _commandLineArgs;
        private string _helpMessage = @"
 Usage: ! <command_name>
 Can be used with the following parameters:

   -h     :  Displays help message.
   -p     :  Uses command with parameters.
                Example: ccs <file_name> -p <parameters>

 NuGet packages:
   Add package directives near the top of a C# file:
                // nuget: Newtonsoft.Json 13.0.3
   xte can create these with :nuget <package> for latest stable, or :nuget add <package> [version].
";

        public void Execute(string args)
        {
            bool hasPipeInput = GlobalVariables.isPipeCommand &&
                      GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp;

            bool hasPipeOutput = GlobalVariables.isPipeCommand &&
                                 GlobalVariables.pipeCmdCount > 0;

            GlobalVariables.isErrorCommand = false;
            _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
            string fileName;
            string param = string.Empty;
            if (args == Name && !GlobalVariables.isPipeCommand)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                return;
            }

            if (args == $"{Name} -h")
            {
                Console.WriteLine(_helpMessage);
                return;
            }

            args = args.Replace("ccs ", "").Trim();

            if (args.ContainsText("-p"))
            {
                fileName = hasPipeInput
                    ? FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), _currentLocation)
                    : FileSystem.SanitizePath(args.SplitByText(" -p ", 0), _currentLocation);
                param = args.SplitByText("-p", 1).Trim();
            }
            else
            {
                fileName = hasPipeInput
                    ? FileSystem.SanitizePath(GlobalVariables.pipeCmdOutput.Trim(), _currentLocation)
                    : FileSystem.SanitizePath(args, _currentLocation);
            }

            CompileAndRun(fileName, param, hasPipeOutput);
            GC.Collect();
        }


        private void CompileAndRun(string fileName, string param, bool capturePipeOutput)
        {
            try
            {
                SplitArguments splitArguments = new SplitArguments(param);
                _commandLineArgs = splitArguments.CommandLineToArgs() ?? Array.Empty<string>();
                ParseCode(fileName);
                Assembly assembly = null;
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(_codeToRun, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
                string assemblyName = Path.GetRandomFileName();
                RoslynReferenceSet referenceSet = GetRef.ReferenceSet(fileName, _codeToRun);
                var references = referenceSet.References;
                foreach (string warning in referenceSet.Warnings)
                    FileSystem.ErrorWriteLine(warning);

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
                            FileSystem.ErrorWriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()} -> line {lineError}");
                        }
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        assembly = Assembly.Load(ms.ToArray());
                    }

                    ms.Close();
                }

                if (assembly == null)
                    return;

                LoadReferencedAssemblies(referenceSet.AssemblyPaths);
                MethodInfo myMethod = assembly.EntryPoint;
                if (!capturePipeOutput)
                {
                    myMethod.Invoke(null, new object[] { _commandLineArgs });
                    return;
                }

                var originalOut = Console.Out;
                GlobalVariables.pipeCmdOutput = string.Empty;

                using var writer = new StringWriter();

                try
                {
                    Console.SetOut(writer);
                    myMethod.Invoke(null, new object[] { _commandLineArgs });
                }
                finally
                {
                    Console.SetOut(originalOut);
                    GlobalVariables.pipeCmdOutput = writer.ToString();
                    GlobalVariables.isErrorCommand = true;
                }
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private static void LoadReferencedAssemblies(IEnumerable<string> assemblyPaths)
        {
            foreach (string path in assemblyPaths)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                        continue;

                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(path);
                    bool alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                    {
                        AssemblyName loadedName = assembly.GetName();
                        return string.Equals(loadedName.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase);
                    });

                    if (!alreadyLoaded)
                        Assembly.LoadFrom(path);
                }
                catch
                {
                }
            }
        }

        private void ParseCode(string fileName)
        {
            try
            {
                fileName = FileSystem.SanitizePath(fileName, _currentLocation);
                if (!File.Exists(fileName))
                {
                    FileSystem.ErrorWriteLine($"File {fileName} does not exist!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                _codeToRun = File.ReadAllText(fileName);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
