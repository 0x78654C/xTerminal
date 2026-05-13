using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using Core;
using Core.DirFiles;

namespace Commands.TerminalCommands.ScriptingLanguage
{
    [SupportedOSPlatform("windows")]
    public class TermXTEditorCommand : ITerminalCommand
    {
        public string Name => "xte";

        private static readonly string s_helpMessage = @"Usage of xte command:
    xte                         : Open an untitled buffer in the current directory.
    xte <file>                  : Open a file in the built-in Vim-style editor.
    xte -new <file>             : Create a template and open it.
    xte -syntax xt <file>       : Use TermXT script syntax highlighting.
    xte -syntax cs <file>       : Use C# syntax highlighting.
    xte -syntax c <file>        : Use C syntax highlighting.
    xte -syntax cpp <file>      : Use C++ syntax highlighting.
    xte -syntax rust <file>     : Use Rust syntax highlighting.
    xte -h                      : Display this help message.

Syntax is selected by extension by default: .xt uses TermXT, .cs/.csx use C#, .c/.h use C, .cpp/.cc/.cxx/.hpp/.hh/.hxx use C++, and .rs uses Rust.

Inside the editor:
    Normal mode : h/j/k/l or arrows move, e opens file explorer, i or Insert enters insert, dd delete line, / search, n or F3 search next.
    Explorer    : Starts from xTerminal current directory and uses fxp controls. Enter opens the selected file in xte.
    Search      : Enter finds, empty Enter repeats the previous search.
    Insert mode : Esc returns to normal mode, Ctrl+Z undo, Ctrl+Y redo.
    Commands    : :e explorer, :w save, :q quit, :q! quit without saving, :wq save and quit.
                  :42 or :goto 42 go to line, :syntax xt|cs|c|cpp|rust switch highlight.
";

        private static readonly string s_template = @"# xTermXT Script template
# Created: {DATE}

set name = ""my-script""

print ""Running {name}...""
run time

print ""Done!""
";

        private static readonly string s_csharpTemplate = @"using System;

public class Program
{
    public static void Main()
    {
        Console.WriteLine(""Hello from xTerminal"");
    }
}
";

        private static readonly string s_cTemplate = @"#include <stdio.h>

int main(void)
{
    printf(""Hello from xTerminal\n"");
    return 0;
}
";

        private static readonly string s_cppTemplate = @"#include <iostream>

int main()
{
    std::cout << ""Hello from xTerminal"" << std::endl;
    return 0;
}
";

        private static readonly string s_rustTemplate = @"fn main() {
    println!(""Hello from xTerminal"");
}
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;

            try
            {
                string rest = args.Length > Name.Length ? args.Substring(Name.Length).TrimStart() : string.Empty;

                if (string.Equals(rest, "-h", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                string currentDir = File.ReadAllText(GlobalVariables.currentDirectory);
                OpenFromArguments(rest, currentDir);
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        internal static void OpenFromArguments(string rest, string currentDir)
        {
            EditorArguments options = ParseEditorArguments(rest);
            string targetArg = options.Target;
            TermXTEditorSyntax syntax = options.SyntaxSpecified
                ? options.Syntax
                : TermXTEditorSyntax.TermXt;

            if (string.IsNullOrWhiteSpace(targetArg))
                targetArg = DefaultUntitledFileName(syntax);

            string target = FileSystem.SanitizePath(TrimMatchingQuotes(targetArg), currentDir);
            if (!options.SyntaxSpecified)
                syntax = TermXTEditor.DetectSyntaxFromPath(target);

            if (options.CreateTemplate && !File.Exists(target))
            {
                string directory = Path.GetDirectoryName(target);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(
                    target,
                    TemplateForSyntax(syntax).Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            var editor = new TermXTEditor(target, syntax);
            editor.Run();
        }

        private static string DefaultUntitledFileName(TermXTEditorSyntax syntax)
        {
            switch (syntax)
            {
                case TermXTEditorSyntax.CSharp:
                    return "untitled.cs";
                case TermXTEditorSyntax.C:
                    return "untitled.c";
                case TermXTEditorSyntax.Cpp:
                    return "untitled.cpp";
                case TermXTEditorSyntax.Rust:
                    return "untitled.rs";
                default:
                    return "untitled.xt";
            }
        }

        private static EditorArguments ParseEditorArguments(string rest)
        {
            var options = new EditorArguments();
            var targetParts = new List<string>();
            List<string> tokens = ParseArguments(rest);

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                if (string.Equals(token, "-new", StringComparison.OrdinalIgnoreCase))
                {
                    options.CreateTemplate = true;
                    continue;
                }

                if (IsSyntaxOption(token))
                {
                    if (i + 1 >= tokens.Count)
                        throw new ArgumentException("Missing syntax value. Use xt, cs, c, cpp, or rust.");

                    options.SetSyntax(ParseSyntax(tokens[++i]));
                    continue;
                }

                string inlineSyntax = GetInlineSyntaxValue(token);
                if (!string.IsNullOrWhiteSpace(inlineSyntax))
                {
                    options.SetSyntax(ParseSyntax(inlineSyntax));
                    continue;
                }

                targetParts.Add(token);
            }

            options.Target = string.Join(" ", targetParts);
            return options;
        }

        private static bool IsSyntaxOption(string token)
        {
            return string.Equals(token, "-syntax", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "-lang", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "-highlight", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetInlineSyntaxValue(string token)
        {
            foreach (string option in new[] { "-syntax=", "-lang=", "-highlight=", "-syntax:", "-lang:", "-highlight:" })
            {
                if (token.StartsWith(option, StringComparison.OrdinalIgnoreCase))
                    return token.Substring(option.Length);
            }

            return string.Empty;
        }

        private static TermXTEditorSyntax ParseSyntax(string value)
        {
            if (TermXTEditor.TryParseSyntax(value, out TermXTEditorSyntax syntax))
                return syntax;

            throw new ArgumentException("Unknown syntax '" + value + "'. Use xt, cs, c, cpp, or rust.");
        }

        private static string TemplateForSyntax(TermXTEditorSyntax syntax)
        {
            switch (syntax)
            {
                case TermXTEditorSyntax.CSharp:
                    return s_csharpTemplate;
                case TermXTEditorSyntax.C:
                    return s_cTemplate;
                case TermXTEditorSyntax.Cpp:
                    return s_cppTemplate;
                case TermXTEditorSyntax.Rust:
                    return s_rustTemplate;
                default:
                    return s_template;
            }
        }

        private static List<string> ParseArguments(string input)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }

                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
                tokens.Add(current.ToString());

            return tokens;
        }

        private static string TrimMatchingQuotes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim();
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                return value.Substring(1, value.Length - 2);

            return value;
        }

        private sealed class EditorArguments
        {
            public bool CreateTemplate { get; set; }
            public bool SyntaxSpecified { get; private set; }
            public TermXTEditorSyntax Syntax { get; private set; }
            public string Target { get; set; }

            public void SetSyntax(TermXTEditorSyntax syntax)
            {
                Syntax = syntax;
                SyntaxSpecified = true;
            }
        }
    }
}
