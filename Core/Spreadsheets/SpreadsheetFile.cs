using System;
using System.IO;
using System.Linq;

namespace Core.Spreadsheets
{
    public static class SpreadsheetFile
    {
        private static readonly string[] s_delimitedExtensions = { ".csv", ".tsv", ".txt" };
        private static readonly string[] s_xlsxExtensions = { ".xlsx", ".xlsm", ".xltx", ".xltm" };

        public static SpreadsheetWorkbook Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Spreadsheet path is empty.", nameof(path));

            path = Path.GetFullPath(path);
            if (!File.Exists(path))
                throw new FileNotFoundException("Spreadsheet file was not found.", path);

            var kind = GetFileKindFromPath(path);
            SpreadsheetWorkbook workbook;

            if (kind == SpreadsheetFileKind.Delimited)
                workbook = DelimitedSpreadsheetFormat.Load(path, GetDelimiterFromPath(path));
            else if (kind == SpreadsheetFileKind.Xlsx)
                workbook = XlsxSpreadsheetFormat.Load(path);
            else if (kind == SpreadsheetFileKind.LegacyXls)
                workbook = LoadLegacyXls(path);
            else
                workbook = DelimitedSpreadsheetFormat.Load(path, GetDelimiterFromPath(path));

            workbook.SourcePath = path;
            workbook.FileKind = kind;
            workbook.Delimiter = GetDelimiterFromPath(path);

            if (workbook.Worksheets.Count == 0)
                workbook.Worksheets.Add(new SpreadsheetWorksheet("Sheet1"));

            return workbook;
        }

        public static void Save(SpreadsheetWorkbook workbook, string path)
        {
            if (workbook == null)
                throw new ArgumentNullException(nameof(workbook));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Spreadsheet path is empty.", nameof(path));

            path = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            foreach (var sheet in workbook.Worksheets)
                sheet.TrimTrailingEmptySpace();

            var kind = GetFileKindFromPath(path);
            if (kind == SpreadsheetFileKind.Delimited)
                DelimitedSpreadsheetFormat.Save(workbook.ActiveWorksheet, path, GetDelimiterFromPath(path));
            else if (kind == SpreadsheetFileKind.LegacyXls)
                SpreadsheetXml2003Format.Save(workbook, path);
            else
                XlsxSpreadsheetFormat.Save(workbook, path);

            workbook.SourcePath = path;
            workbook.FileKind = kind;
            workbook.Delimiter = GetDelimiterFromPath(path);
        }

        public static SpreadsheetFileKind GetFileKindFromPath(string path)
        {
            var extension = Path.GetExtension(path ?? string.Empty).ToLowerInvariant();
            if (s_delimitedExtensions.Contains(extension))
                return SpreadsheetFileKind.Delimited;

            if (s_xlsxExtensions.Contains(extension))
                return SpreadsheetFileKind.Xlsx;

            if (extension == ".xls")
                return SpreadsheetFileKind.LegacyXls;

            return SpreadsheetFileKind.Unknown;
        }

        public static char GetDelimiterFromPath(string path)
        {
            var extension = Path.GetExtension(path ?? string.Empty).ToLowerInvariant();
            if (extension == ".tsv")
                return '\t';

            return ',';
        }

        private static SpreadsheetWorkbook LoadLegacyXls(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length >= 2 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K')
                return XlsxSpreadsheetFormat.Load(path);

            if (LooksLikeText(bytes))
            {
                var text = File.ReadAllText(path);
                if (SpreadsheetXml2003Format.TryLoad(text, path, out var xmlWorkbook))
                    return xmlWorkbook;

                return DelimitedSpreadsheetFormat.Load(path, DelimitedSpreadsheetFormat.DetectDelimiter(text, ','));
            }

            return LegacyBinaryXlsFormat.Load(path, bytes);
        }

        private static bool LooksLikeText(byte[] bytes)
        {
            if (bytes.Length == 0)
                return true;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return true;

            if (bytes.Length >= 2
                && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)))
                return true;

            if (bytes[0] == (byte)'<' || bytes[0] == (byte)'\t' || bytes[0] == (byte)'"')
                return true;

            int scanLength = Math.Min(bytes.Length, 512);
            for (int i = 0; i < scanLength; i++)
            {
                if (bytes[i] == 0)
                    return false;
            }

            return true;
        }
    }
}
