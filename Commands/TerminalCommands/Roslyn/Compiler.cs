using Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Commands.TerminalCommands.Roslyn
{
    public class Compiler : ITerminalCommand
    {
        /*
         Compiles C# in memory usint Roslyn 
         */
        public string Name => "ccs";
        private string _codeToRun;
        private string _namesapce;
        private string _className;
        private string _currentLocation = string.Empty;
        public void Execute(string args)
        {
            _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
            string fileName;
            string param = string.Empty;
            if (args.Length == 3)
            {
                FileSystem.ErrorWriteLine($"You must provide an C# file for complile.");
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
                ParseCode(fileName);
                Assembly assembly = null;
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(_codeToRun);
                string assemblyName = Path.GetRandomFileName();
                string assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
                var references = Directory.GetFiles(assemblyPath).Where(t => t.EndsWith(".dll"))
        .Where(t => Core.SystemTools.Roslyn.IsManaged(t))
        .Select(t => MetadataReference.CreateFromFile(t)).ToArray();

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
                references = null;
                compilation = null;
                assembly = null;
                type = null;
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
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
