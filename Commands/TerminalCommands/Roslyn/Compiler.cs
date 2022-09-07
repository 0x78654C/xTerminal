﻿using Core;
using Core.SystemTools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GetRef = Core.SystemTools.Roslyn;

namespace Commands.TerminalCommands.Roslyn
{
    public class Compiler : ITerminalCommand
    {
        /*
         Compiles C# in memory using Roslyn 
         */
        public string Name => "ccs";
        private string _codeToRun;
        private string _currentLocation = string.Empty;
        private string[] _commandLineArgs;
        public void Execute(string args)
        {
            _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
            string fileName;
            string param = string.Empty;
            if (args.Length == 3)
            {
                FileSystem.ErrorWriteLine($"You must provide a C# file for compilation.");
                return;
            }

            args = args.Replace("ccs ", "");
            if (args.ContainsText("-p"))
            {
                fileName = FileSystem.SanitizePath(args.SplitByText(" -p ", 0), _currentLocation);
                param = args.SplitByText(" -p ", 1);
                CompileAndRun(fileName, param);
                GC.Collect();
                return;
            }
            args = FileSystem.SanitizePath(args, _currentLocation);
            CompileAndRun(args, param);
            GC.Collect();
        }


        private void CompileAndRun(string fileName, string param)
        {
            try
            {
                SplitArguments splitArguments = new SplitArguments(param);
                _commandLineArgs = splitArguments.CommandLineToArgs() ?? Array.Empty<string>();
                ParseCode(fileName);
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
                            Console.Error.WriteLine("{0}: {1} -> line {2}", diagnostic.Id, diagnostic.GetMessage(),lineError);
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

        private void ParseCode(string fileName)
        {
            try
            {
                fileName = FileSystem.SanitizePath(fileName, _currentLocation);
                if (!File.Exists(fileName))
                {
                    FileSystem.ErrorWriteLine($"File {fileName} does not exist!");
                    return;
                }
                _codeToRun = File.ReadAllText(fileName);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
