using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.Spreadsheets
{
    internal static class DelimitedSpreadsheetFormat
    {
        public static SpreadsheetWorkbook Load(string path, char preferredDelimiter)
        {
            var text = File.ReadAllText(path);
            var delimiter = DetectDelimiter(text, preferredDelimiter);
            var workbook = new SpreadsheetWorkbook
            {
                SourcePath = path,
                FileKind = SpreadsheetFileKind.Delimited,
                Delimiter = delimiter
            };

            var sheetName = Path.GetFileNameWithoutExtension(path);
            var sheet = new SpreadsheetWorksheet(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName);
            foreach (var row in Parse(text, delimiter))
                sheet.AddRow(row);

            workbook.Worksheets.Add(sheet);
            return workbook;
        }

        public static void Save(SpreadsheetWorksheet sheet, string path, char delimiter)
        {
            var rowCount = sheet.UsedRowCount;
            var columnCount = sheet.UsedColumnCount;
            var builder = new StringBuilder();

            for (int row = 0; row < rowCount; row++)
            {
                if (row > 0)
                    builder.AppendLine();

                for (int col = 0; col < columnCount; col++)
                {
                    if (col > 0)
                        builder.Append(delimiter);

                    builder.Append(Escape(sheet.GetCell(row, col), delimiter));
                }
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        }

        public static char DetectDelimiter(string text, char preferredDelimiter)
        {
            if (preferredDelimiter == '\t')
                return '\t';

            int commaScore = CountDelimiter(text, ',');
            int semicolonScore = CountDelimiter(text, ';');
            int tabScore = CountDelimiter(text, '\t');

            if (tabScore > commaScore && tabScore > semicolonScore)
                return '\t';

            if (semicolonScore > commaScore)
                return ';';

            return preferredDelimiter == '\0' ? ',' : preferredDelimiter;
        }

        private static List<List<string>> Parse(string text, char delimiter)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(ch);
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                    continue;
                }

                if (ch == delimiter)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    row.Add(field.ToString());
                    rows.Add(row);
                    row = new List<string>();
                    field.Clear();
                    continue;
                }

                field.Append(ch);
            }

            if (field.Length > 0 || row.Count > 0 || text.Length == 0 || text[text.Length - 1] == delimiter)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }

        private static string Escape(string value, char delimiter)
        {
            value = value ?? string.Empty;
            bool mustQuote = value.IndexOf(delimiter) >= 0
                             || value.IndexOf('"') >= 0
                             || value.IndexOf('\r') >= 0
                             || value.IndexOf('\n') >= 0
                             || (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])));

            if (!mustQuote)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static int CountDelimiter(string text, char delimiter)
        {
            int score = 0;
            int lines = 0;
            bool inQuotes = false;

            for (int i = 0; i < text.Length && lines < 20; i++)
            {
                char ch = text[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes && ch == delimiter)
                    score++;
                else if (!inQuotes && (ch == '\r' || ch == '\n'))
                {
                    lines++;
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                }
            }

            return score;
        }
    }
}
