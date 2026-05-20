using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Spreadsheets
{
    public enum SpreadsheetFileKind
    {
        Unknown,
        Delimited,
        Xlsx,
        LegacyXls
    }

    public sealed class SpreadsheetWorkbook
    {
        public SpreadsheetWorkbook()
        {
            Worksheets = new List<SpreadsheetWorksheet>();
            ActiveSheetIndex = 0;
            SourcePath = string.Empty;
            FileKind = SpreadsheetFileKind.Unknown;
            Delimiter = ',';
        }

        public List<SpreadsheetWorksheet> Worksheets { get; }

        public int ActiveSheetIndex { get; set; }

        public string SourcePath { get; set; }

        public SpreadsheetFileKind FileKind { get; set; }

        public char Delimiter { get; set; }

        public SpreadsheetWorksheet ActiveWorksheet
        {
            get
            {
                if (Worksheets.Count == 0)
                    Worksheets.Add(new SpreadsheetWorksheet("Sheet1"));

                if (ActiveSheetIndex < 0)
                    ActiveSheetIndex = 0;
                if (ActiveSheetIndex >= Worksheets.Count)
                    ActiveSheetIndex = Worksheets.Count - 1;

                return Worksheets[ActiveSheetIndex];
            }
        }

        public static SpreadsheetWorkbook CreateBlank(string sourcePath)
        {
            var workbook = new SpreadsheetWorkbook
            {
                SourcePath = sourcePath,
                FileKind = SpreadsheetFile.GetFileKindFromPath(sourcePath),
                Delimiter = SpreadsheetFile.GetDelimiterFromPath(sourcePath)
            };

            workbook.Worksheets.Add(new SpreadsheetWorksheet(
                string.IsNullOrWhiteSpace(sourcePath)
                    ? "Sheet1"
                    : Path.GetFileNameWithoutExtension(sourcePath)));

            return workbook;
        }
    }

    public sealed class SpreadsheetWorksheet
    {
        private readonly List<List<string>> _rows;

        public SpreadsheetWorksheet(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Sheet1" : name.Trim();
            _rows = new List<List<string>>();
        }

        public string Name { get; set; }

        public int RowCount => Math.Max(1, _rows.Count);

        public int ColumnCount
        {
            get
            {
                if (_rows.Count == 0)
                    return 1;

                var max = _rows.Max(row => row.Count);
                return Math.Max(1, max);
            }
        }

        public int UsedRowCount
        {
            get
            {
                for (int row = _rows.Count - 1; row >= 0; row--)
                {
                    if (_rows[row].Any(value => !string.IsNullOrEmpty(value)))
                        return row + 1;
                }

                return 1;
            }
        }

        public int UsedColumnCount
        {
            get
            {
                int max = 0;
                foreach (var row in _rows)
                {
                    for (int col = row.Count - 1; col >= 0; col--)
                    {
                        if (!string.IsNullOrEmpty(row[col]))
                        {
                            max = Math.Max(max, col + 1);
                            break;
                        }
                    }
                }

                return Math.Max(1, max);
            }
        }

        public string GetCell(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || columnIndex < 0)
                return string.Empty;

            if (rowIndex >= _rows.Count)
                return string.Empty;

            var row = _rows[rowIndex];
            if (columnIndex >= row.Count)
                return string.Empty;

            return row[columnIndex] ?? string.Empty;
        }

        public void SetCell(int rowIndex, int columnIndex, string value)
        {
            if (rowIndex < 0 || columnIndex < 0)
                return;

            EnsureCell(rowIndex, columnIndex);
            _rows[rowIndex][columnIndex] = value ?? string.Empty;
        }

        public void AddRow(IList<string> values)
        {
            var row = new List<string>();
            if (values != null)
            {
                foreach (var value in values)
                    row.Add(value ?? string.Empty);
            }

            _rows.Add(row);
        }

        public void InsertRow(int rowIndex)
        {
            rowIndex = Math.Max(0, Math.Min(rowIndex, _rows.Count));
            int columns = ColumnCount;
            var row = new List<string>(columns);
            for (int col = 0; col < columns; col++)
                row.Add(string.Empty);

            _rows.Insert(rowIndex, row);
        }

        public void DeleteRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rows.Count)
                return;

            _rows.RemoveAt(rowIndex);
            if (_rows.Count == 0)
                _rows.Add(new List<string>());
        }

        public void InsertColumn(int columnIndex)
        {
            columnIndex = Math.Max(0, Math.Min(columnIndex, ColumnCount));
            if (_rows.Count == 0)
                _rows.Add(new List<string>());

            foreach (var row in _rows)
            {
                while (row.Count < columnIndex)
                    row.Add(string.Empty);
                row.Insert(columnIndex, string.Empty);
            }
        }

        public void DeleteColumn(int columnIndex)
        {
            if (columnIndex < 0)
                return;

            foreach (var row in _rows)
            {
                if (columnIndex < row.Count)
                    row.RemoveAt(columnIndex);
            }

            if (_rows.Count == 0)
                _rows.Add(new List<string>());
        }

        public List<List<string>> GetValues(int rowCount, int columnCount)
        {
            rowCount = Math.Max(1, rowCount);
            columnCount = Math.Max(1, columnCount);

            var values = new List<List<string>>(rowCount);
            for (int row = 0; row < rowCount; row++)
            {
                var line = new List<string>(columnCount);
                for (int col = 0; col < columnCount; col++)
                    line.Add(GetCell(row, col));

                values.Add(line);
            }

            return values;
        }

        public void TrimTrailingEmptySpace()
        {
            for (int row = _rows.Count - 1; row >= 0; row--)
            {
                TrimRow(_rows[row]);
                if (_rows[row].Count == 0)
                    _rows.RemoveAt(row);
                else
                    break;
            }

            foreach (var row in _rows)
                TrimRow(row);
        }

        private void EnsureCell(int rowIndex, int columnIndex)
        {
            while (_rows.Count <= rowIndex)
                _rows.Add(new List<string>());

            var row = _rows[rowIndex];
            while (row.Count <= columnIndex)
                row.Add(string.Empty);
        }

        private static void TrimRow(List<string> row)
        {
            for (int col = row.Count - 1; col >= 0; col--)
            {
                if (!string.IsNullOrEmpty(row[col]))
                    return;

                row.RemoveAt(col);
            }
        }
    }
}
