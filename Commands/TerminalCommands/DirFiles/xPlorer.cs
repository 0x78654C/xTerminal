using Core;
using Core.DirFiles;
using System.IO;
using System;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class xPlorer : ITerminalCommand
    {
        public string Name => "fxp";
        public void Execute(string args)
        {
            string argument = GetParameterText(args);
            if (argument == "-h" || argument == "--help")
            {
                Console.WriteLine("fxp [folder] - Open the console file explorer. Quote paths containing spaces.");
                Console.WriteLine("Arrows / mouse wheel: move; Ctrl+Up/Down: page; Home/End: first/last.");
                Console.WriteLine("PgDn / Ctrl+PgUp: page down/up; PgUp: parent folder; Backspace: back.");
                Console.WriteLine("Enter: open; /: search; Tab: drives; Del: delete; F5: refresh; Esc or `: quit.");
                return;
            }

            try
            {
                string path = ResolveStartPath(File.ReadAllText(GlobalVariables.currentDirectory), argument);
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("fxp: Folder not found: " + path);
                    return;
                }
                new FileExplorer(path).Run();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is ArgumentException || ex is NotSupportedException)
            {
                Console.WriteLine("fxp: " + ex.Message);
            }
        }

        private string GetParameterText(string commandLine)
        {
            string text = (commandLine ?? "").Trim();
            // The shell passes the entire command line, including "fxp".
            if (text.StartsWith(Name, StringComparison.OrdinalIgnoreCase) &&
                (text.Length == Name.Length || char.IsWhiteSpace(text[Name.Length])))
                return text.Substring(Name.Length).TrimStart();
            return text;
        }

        private static string ResolveStartPath(string currentDirectory, string argument)
        {
            string path = (argument ?? "").Trim();
            if (path.Length >= 2 && path[0] == '"' && path[path.Length - 1] == '"')
                path = path.Substring(1, path.Length - 2);
            string root = Path.GetFullPath(currentDirectory.Trim());
            return path.Length == 0 ? root : Path.GetFullPath(path, root);
        }
    }
}
