using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Spreadsheets
{
    public static class SpreadsheetLimits
    {
        public const int MaxRows = 100000;
        public const int MaxColumns = 16384;
        public const int MaxMaterializedCells = 2000000;
        public const int MaxWorksheets = 256;
        public const long MaxFileBytes = 64L * 1024 * 1024;
        public const long MaxArchiveEntryBytes = 32L * 1024 * 1024;
        public const long MaxArchiveExpandedBytes = 128L * 1024 * 1024;
        public const int MaxCompressionRatio = 100;

        public static void ValidateCellIndex(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= MaxRows)
                throw new ArgumentOutOfRangeException(nameof(rowIndex), $"Spreadsheet rows are limited to {MaxRows}.");
            if (columnIndex < 0 || columnIndex >= MaxColumns)
                throw new ArgumentOutOfRangeException(nameof(columnIndex), $"Spreadsheet columns are limited to {MaxColumns}.");
        }
    }

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
        private int _materializedCellCount;

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
            SpreadsheetLimits.ValidateCellIndex(rowIndex, columnIndex);

            EnsureCell(rowIndex, columnIndex);
            _rows[rowIndex][columnIndex] = value ?? string.Empty;
        }

        public void AddRow(IList<string> values)
        {
            if (_rows.Count >= SpreadsheetLimits.MaxRows)
                throw new InvalidDataException($"Spreadsheet rows are limited to {SpreadsheetLimits.MaxRows}.");
            if (values != null && values.Count > SpreadsheetLimits.MaxColumns)
                throw new InvalidDataException($"Spreadsheet columns are limited to {SpreadsheetLimits.MaxColumns}.");
            if (values != null && (long)_materializedCellCount + values.Count > SpreadsheetLimits.MaxMaterializedCells)
                throw new InvalidDataException("Spreadsheet cell limit exceeded.");

            var row = new List<string>();
            if (values != null)
            {
                foreach (var value in values)
                    row.Add(value ?? string.Empty);
            }

            _rows.Add(row);
            _materializedCellCount += row.Count;
        }

        public void InsertRow(int rowIndex)
        {
            if (_rows.Count >= SpreadsheetLimits.MaxRows)
                throw new InvalidDataException($"Spreadsheet rows are limited to {SpreadsheetLimits.MaxRows}.");
            rowIndex = Math.Max(0, Math.Min(rowIndex, _rows.Count));
            int columns = ColumnCount;
            if ((long)_materializedCellCount + columns > SpreadsheetLimits.MaxMaterializedCells)
                throw new InvalidDataException("Spreadsheet cell limit exceeded.");
            var row = new List<string>(columns);
            for (int col = 0; col < columns; col++)
                row.Add(string.Empty);

            _rows.Insert(rowIndex, row);
            _materializedCellCount += row.Count;
        }

        public void DeleteRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rows.Count)
                return;

            _materializedCellCount -= _rows[rowIndex].Count;
            _rows.RemoveAt(rowIndex);
            if (_rows.Count == 0)
                _rows.Add(new List<string>());
        }

        public void InsertColumn(int columnIndex)
        {
            if (ColumnCount >= SpreadsheetLimits.MaxColumns)
                throw new InvalidDataException($"Spreadsheet columns are limited to {SpreadsheetLimits.MaxColumns}.");
            columnIndex = Math.Max(0, Math.Min(columnIndex, ColumnCount));
            if (_rows.Count == 0)
                _rows.Add(new List<string>());

            long cellsToAdd = 0;
            foreach (var row in _rows)
                cellsToAdd += Math.Max(0, columnIndex - row.Count) + 1;
            if (_materializedCellCount + cellsToAdd > SpreadsheetLimits.MaxMaterializedCells)
                throw new InvalidDataException("Spreadsheet cell limit exceeded.");

            foreach (var row in _rows)
            {
                int previousCount = row.Count;
                while (row.Count < columnIndex)
                    row.Add(string.Empty);
                row.Insert(columnIndex, string.Empty);
                _materializedCellCount += row.Count - previousCount;
            }
        }

        public void DeleteColumn(int columnIndex)
        {
            if (columnIndex < 0)
                return;

            foreach (var row in _rows)
            {
                if (columnIndex < row.Count)
                {
                    row.RemoveAt(columnIndex);
                    _materializedCellCount--;
                }
            }

            if (_rows.Count == 0)
                _rows.Add(new List<string>());
        }

        public List<List<string>> GetValues(int rowCount, int columnCount)
        {
            rowCount = Math.Max(1, rowCount);
            columnCount = Math.Max(1, columnCount);
            SpreadsheetLimits.ValidateCellIndex(rowCount - 1, columnCount - 1);
            if ((long)rowCount * columnCount > SpreadsheetLimits.MaxMaterializedCells)
                throw new InvalidDataException("Spreadsheet cell limit exceeded.");

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

            _materializedCellCount = _rows.Sum(row => row.Count);
        }

        private void EnsureCell(int rowIndex, int columnIndex)
        {
            int existingColumns = rowIndex < _rows.Count ? _rows[rowIndex].Count : 0;
            int cellsToAdd = Math.Max(0, columnIndex + 1 - existingColumns);
            if ((long)_materializedCellCount + cellsToAdd > SpreadsheetLimits.MaxMaterializedCells)
                throw new InvalidDataException("Spreadsheet cell limit exceeded.");

            while (_rows.Count <= rowIndex)
                _rows.Add(new List<string>());

            var row = _rows[rowIndex];
            while (row.Count <= columnIndex)
                row.Add(string.Empty);
            _materializedCellCount += cellsToAdd;
        }

        private static void TrimRow(List<string> row)
        {
            for (int col = row.Count - 1; col >= 0; col--)
            {
                if (!string.IsNullOrEmpty(row[col]))
                    return;

                row.RemoveAt(col);
                // The count is recalculated by the caller after all rows are trimmed.
            }
        }
    }
}
