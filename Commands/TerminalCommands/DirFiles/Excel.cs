using Core;
using Core.Spreadsheets;
using Core.SystemTools;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class Excel : ITerminalCommand
    {
        public string Name => "xcel";

        private static readonly string s_helpMessage = @"Usage of xcel command:
  xcel <file_path>  : Open a spreadsheet in the terminal grid editor.
  xcel -h           : Display this message.

Supported files:
  .xlsx, .xlsm, .xltx, .xltm, .xls, .csv, .tsv, .txt

Editor keys:
  Arrows       Move through rows and columns.
  Shift+Arrows Select a cell range.
  Shift+Home   Move to the top row.
  Shift+End    Move to the last used row.
  Mouse drag   Select cells or row headers.
  Mouse wheel  Scroll rows. Shift+wheel scrolls columns.
  Column click Select the entire column.
  Ctrl+C       Copy selected cells without row or column headers.
  Enter/F2     Edit the selected cell.
  Del          Clear the selected cell.
  Ins          Insert a row below the selected row.
  Ctrl+Ins     Insert a column to the right of the selected column.
  Tab          Switch worksheet.
  Ctrl+N       Add worksheet.
  Ctrl+S       Save.
  F5           Jump to a cell like B12.
  Esc/Q        Quit.

Note:
  .xls files are handled by xTerminal's own reader. Saving .xls writes Excel XML
  format with the .xls extension so Excel can still open and edit it.";

        public void Execute(string args)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                string currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

                string parameterText = GetParameterText(args);
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0)
                    parameterText = GlobalVariables.pipeCmdOutput.Trim();

                if (string.IsNullOrWhiteSpace(parameterText) || parameterText.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                string fileArgument = GetFileArgument(parameterText);
                if (string.IsNullOrWhiteSpace(fileArgument))
                {
                    FileSystem.ErrorWriteLine("You must provide a spreadsheet file path.");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }

                string filePath = FileSystem.SanitizePath(fileArgument, currentDirectory);
                SpreadsheetWorkbook workbook;
                if (File.Exists(filePath))
                {
                    workbook = SpreadsheetFile.Load(filePath);
                }
                else
                {
                    if (SpreadsheetFile.GetFileKindFromPath(filePath) == SpreadsheetFileKind.Unknown)
                    {
                        FileSystem.ErrorWriteLine("Unsupported spreadsheet extension. Use .xlsx, .xls, .csv, .tsv, or .txt.");
                        GlobalVariables.isErrorCommand = true;
                        return;
                    }

                    workbook = SpreadsheetWorkbook.CreateBlank(filePath);
                }

                var editor = new SpreadsheetGridEditor(workbook, filePath);
                editor.Run();
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        private string GetParameterText(string args)
        {
            args = args ?? string.Empty;
            args = args.Trim();

            if (args.Equals(Name, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (args.StartsWith(Name + " ", StringComparison.OrdinalIgnoreCase))
                return args.Substring(Name.Length).Trim();

            return args;
        }

        private static string GetFileArgument(string parameterText)
        {
            try
            {
                var arguments = new SplitArguments(parameterText).CommandLineToArgs();
                return arguments.Length == 0 ? string.Empty : arguments[0];
            }
            catch
            {
                return parameterText.Trim().Trim('"');
            }
        }
    }
}
