using System;
using System.IO;
using System.Runtime.Versioning;
using Core;
using Core.DirFiles;

namespace Commands.TerminalCommands.ScriptingLanguage
{
    [SupportedOSPlatform("windows")]
    public class TermXTEditorCommand : ITerminalCommand
    {
        public string Name => "xte";

        private static readonly string s_helpMessage = @"Usage of xte command:
    xte <script.xt>        : Open a TermXT script in the built-in Vim-style editor.
    xte -new <script.xt>   : Create a TermXT script template and open it.
    xte -h                 : Display this help message.

Inside the editor:
    Normal mode : h/j/k/l or arrows move, i or Insert enters insert, dd delete line, / search.
    Insert mode : Esc returns to normal mode, Ctrl+S saves current data, Ctrl+U undo.
    Commands    : :w save, :q quit, :q! quit without saving, :wq save and quit.
";

        private static readonly string s_template = @"# xTermXT Script template
# Created: {DATE}

set name = ""my-script""

print ""Running {name}...""
run time

print ""Done!""
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;

            try
            {
                string rest = args.Length > Name.Length ? args.Substring(Name.Length).TrimStart() : string.Empty;

                if (string.Equals(rest, "-h", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(rest))
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
            bool createTemplate = rest.StartsWith("-new ", StringComparison.OrdinalIgnoreCase);
            string targetArg = createTemplate ? rest.Substring(5).Trim() : rest.Trim();

            if (string.IsNullOrWhiteSpace(targetArg))
                throw new ArgumentException("You must provide a script file path.");

            string target = FileSystem.SanitizePath(TrimMatchingQuotes(targetArg), currentDir);

            if (createTemplate && !File.Exists(target))
            {
                string directory = Path.GetDirectoryName(target);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(
                    target,
                    s_template.Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            var editor = new TermXTEditor(target);
            editor.Run();
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
    }
}
