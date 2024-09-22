using Core;
using System;
using System.Runtime.Versioning;
using System.Text;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("windows")]
    public class ConsoleEncoding : ITerminalCommand
    {
        public string Name => "enc";
        private static string s_helpMessage = $@" Usage of enc command:
    enc defalut  : Set input/output encoding to system default .NET encoding.
    enc utf8     : Set input/output encoding to system UTF8.
    enc unicode  : Set input/output encoding to system Unicode.
    enc ascii    : Set input/output encoding to system ascii.
    enc -current : Show the current input/output encoding.  
";
        public void Execute(string arg)
        {
            if (arg == Name)
            {
                FileSystem.SuccessWriteLine("Use -h for more information!");
                return;
            }

            arg = arg.Substring(3).Trim().ToLower();

            // Display help message.
            if (arg.Trim() == "-h")
            {
                Console.WriteLine(s_helpMessage);
                return;
            }

            if(arg == "-current")
            {
                var encodingIn = Console.InputEncoding.EncodingName;
                var encodingOut = Console.OutputEncoding.EncodingName;
                var dataDisplay = $"Input encoding: {encodingIn}\nOutput encoding: {encodingOut}";
                FileSystem.SuccessWriteLine(dataDisplay);
                return;
            }

            if (arg == "utf8")
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
                FileSystem.SuccessWriteLine($"Console encoding set to: {Console.InputEncoding.EncodingName}");
                return;
            }

            if (arg == "unicode")
            {
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                FileSystem.SuccessWriteLine($"Console encoding set to: {Console.InputEncoding.EncodingName}");
                return;
            }

            if (arg == "default")
            {
                Console.InputEncoding = Encoding.Default;
                Console.OutputEncoding = Encoding.Default;
                FileSystem.SuccessWriteLine($"Console encoding set to: {Console.InputEncoding.EncodingName}");
                return;
            }

            if (arg == "ascii")
            {
                Console.InputEncoding = Encoding.ASCII;
                Console.OutputEncoding = Encoding.ASCII;
                FileSystem.SuccessWriteLine($"Console encoding set to: {Console.InputEncoding.EncodingName}");
                return;
            }
            FileSystem.ErrorWriteLine("The encode type is not part of the lis. Use -h for more information!");
        }
    }
}
