using Core;
using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class XClip : ITerminalCommand
    {
        public string Name => "xclip";

        private static string s_helpMessage = @"Usage of xclip command:
    <command> | xclip                : Copies previous command output to the clipboard.
    <command> | xclip | <command>    : Copies previous command output and passes it to the next command.
    xclip <text>                     : Copies the provided text to the clipboard.
    xclip -h                         : Displays this help message.

Example:
    ls | xclip
    ls | xclip | cat -s .txt
";

        public void Execute(string args)
        {
            GlobalVariables.isErrorCommand = false;

            try
            {
                string input = GetInput(args);

                if (input == "-h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (GlobalVariables.isPipeCommand)
                    input = GlobalVariables.pipeCmdOutput ?? string.Empty;

                if (string.IsNullOrEmpty(input))
                {
                    FileSystem.ErrorWriteLine("No data available to copy. Pipe command output to xclip or provide text.");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                SetClipboardText(input);

                if (GlobalVariables.isPipeCommand)
                    GlobalVariables.pipeCmdOutput = input;

                if (!GlobalVariables.isPipeCommand)
                    FileSystem.SuccessWriteLine("Data copied to clipboard.");
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }


        /// <summary>
        /// Gets the input text for the xclip command. If the input is empty, it returns an empty string. 
        /// If the input starts with the command name, it removes the command name and trims the remaining text. 
        /// If the input is enclosed in double quotes, it removes the quotes.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string GetInput(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                return string.Empty;

            string input = args.Trim();
            if (input.Equals(Name, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            input = input.Length > Name.Length
                ? input.Substring(Name.Length).Trim()
                : string.Empty;

            if (input.Length >= 2 && input[0] == '"' && input[input.Length - 1] == '"')
                input = input.Substring(1, input.Length - 2);

            return input;
        }

        /// <summary>
        /// Set the clipboard text using a separate STA thread to avoid COM exceptions when the clipboard is in use by another process.
        /// </summary>
        /// <param name="text"></param>
        private static void SetClipboardText(string text)
        {
            Exception failure = null;

            for (int i = 0; i < 5; i++)
            {
                failure = null;

                var thread = new Thread(() =>
                {
                    try
                    {
                        Clipboard.SetText(text, TextDataFormat.UnicodeText);
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (failure == null)
                    return;

                Thread.Sleep(50);
            }

            throw failure;
        }
    }
}
