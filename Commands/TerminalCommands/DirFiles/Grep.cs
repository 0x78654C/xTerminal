using System;
using Core;
using Core.DirFiles;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class Grep : ITerminalCommand
    {
        private static readonly string s_helpMessage = @"Usage: grep [OPTIONS] PATTERN [FILE ...]
       command | grep [OPTIONS] PATTERN

Search text files or piped input, printing each selected line.
  -i, --ignore-case          Ignore case.
  -v, --invert-match         Select nonmatching lines.
  -n, --line-number          Prefix lines with their 1-based line number.
  -r, --recursive            Search directories recursively (default: current directory).
  -F, --fixed-strings        Match literal text.
  -E, --extended-regexp      Match .NET regular expressions (the default).
  -w, --word-regexp          Match whole words.
  -x, --line-regexp          Match whole lines.
  -e, --regexp PATTERN       Add a pattern; may be repeated.
  -f, --file FILE            Read patterns from a file, one per line.
  -c, --count                Print selected line counts per file.
  -l, --files-with-matches   Print names of files with selected lines.
  -L, --files-without-match  Print names of files without selected lines.
  -o, --only-matching        Print only nonempty matching parts.
  -q, --quiet                Suppress output; stop on the first selected line.
  -s, --no-messages          Suppress file access errors.
  -H, --with-filename        Always prefix output with the filename.
  -h, --no-filename          Suppress filename prefixes (use --help for help).
  -m, --max-count N          Stop after N selected lines per file.
  -A, --after-context N      Show N lines after a match.
  -B, --before-context N     Show N lines before a match.
  -C, --context N           Show N lines before and after a match.
      --help                Show this message.
      --                    End options, allowing a pattern starting with '-'.

Short flags can be combined: -inv. Quote patterns and paths containing spaces.
FILE supports * and ? in the filename; '-' reads piped or redirected input.
Text is read as UTF-8, with UTF-8/UTF-16/UTF-32 BOM detection. Recursive search
skips discovered symbolic links and junctions. Ctrl+X cancels a running search.
Regex syntax uses .NET rather than GNU basic regular expressions.

Examples:
  grep -in error app.log
  grep -rn -F TODO .
  grep -E 'error|warning' *.log
  grep -n -C2 'failed request' 'server log.txt'
  cat app.log | grep -i error | grep -v debug
  grep error app.log | tee matches.txt
";
        public string Name => "grep";
        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;
            bool wasCancelable = GlobalVariables.eventKeyFlagX;
            bool hasPipeInput = GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp;
            string pipeInput = hasPipeInput ? GlobalVariables.pipeCmdOutput ?? string.Empty : null;
            bool captureOutput = GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0;
            using var captured = new StringWriter();
            using var pipedReader = pipeInput == null ? null : new StringReader(pipeInput);
            TextWriter output = captureOutput ? captured : Console.Out;
            if (GlobalVariables.isPipeCommand) GlobalVariables.pipeCmdOutput = string.Empty;
            try
            {
                string arguments = (args ?? string.Empty).TrimStart();
                if (arguments == Name) arguments = string.Empty;
                else if (arguments.StartsWith(Name, StringComparison.Ordinal) && arguments.Length > Name.Length && char.IsWhiteSpace(arguments[Name.Length]))
                    arguments = arguments[Name.Length..].TrimStart();
                GrepManage.Options options = GrepManage.ParseArguments(arguments);
                if (options.Help) { output.WriteLine(s_helpMessage); return; }

                string directory = File.Exists(GlobalVariables.currentDirectory)
                    ? File.ReadAllText(GlobalVariables.currentDirectory).Trim()
                    : Directory.GetCurrentDirectory();
                TextReader input = hasPipeInput ? pipedReader : Console.IsInputRedirected ? Console.In : null;
                GlobalVariables.eventCancelKey = false;
                GlobalVariables.eventKeyFlagX = true;
                int status = GrepManage.Search(options, directory, input, output, Console.Error,
                    () => GlobalVariables.eventCancelKey);
                GlobalVariables.isErrorCommand = status != 0;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine("grep: " + ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
            finally
            {
                if (captureOutput) GlobalVariables.pipeCmdOutput = captured.ToString();
                GlobalVariables.eventKeyFlagX = wasCancelable;
                GlobalVariables.eventCancelKey = false;
            }
        }
    }
}
