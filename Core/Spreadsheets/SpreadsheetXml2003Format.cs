using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Core.Spreadsheets
{
    internal static class SpreadsheetXml2003Format
    {
        private static readonly XNamespace SpreadsheetNs = "urn:schemas-microsoft-com:office:spreadsheet";

        public static bool TryLoad(string text, string path, out SpreadsheetWorkbook workbook)
        {
            workbook = null;
            if (string.IsNullOrWhiteSpace(text) || text.IndexOf("<Workbook", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            try
            {
                var document = XDocument.Parse(text, LoadOptions.None);
                if (!string.Equals(document.Root.Name.LocalName, "Workbook", StringComparison.OrdinalIgnoreCase))
                    return false;

                workbook = new SpreadsheetWorkbook
                {
                    SourcePath = path,
                    FileKind = SpreadsheetFileKind.LegacyXls
                };

                int totalCells = 0;
                foreach (var worksheetElement in document.Root.Elements(SpreadsheetNs + "Worksheet"))
                {
                    if (workbook.Worksheets.Count >= SpreadsheetLimits.MaxWorksheets)
                        throw new InvalidDataException($"Spreadsheet worksheets are limited to {SpreadsheetLimits.MaxWorksheets}.");
                    string name = (string)worksheetElement.Attribute(SpreadsheetNs + "Name") ?? "Sheet";
                    var worksheet = new SpreadsheetWorksheet(name);
                    int rowIndex = 0;

                    var table = worksheetElement.Element(SpreadsheetNs + "Table");
                    if (table == null)
                        continue;

                    foreach (var rowElement in table.Elements(SpreadsheetNs + "Row"))
                    {
                        var rowIndexAttribute = (string)rowElement.Attribute(SpreadsheetNs + "Index");
                        if (!string.IsNullOrWhiteSpace(rowIndexAttribute))
                        {
                            if (!int.TryParse(rowIndexAttribute, NumberStyles.Integer, CultureInfo.InvariantCulture, out var oneBasedRowIndex)
                                || oneBasedRowIndex <= 0)
                                throw new InvalidDataException("Invalid SpreadsheetML row index: " + rowIndexAttribute);
                            rowIndex = oneBasedRowIndex - 1;
                        }
                        if (rowIndex >= SpreadsheetLimits.MaxRows)
                            throw new InvalidDataException($"Spreadsheet rows are limited to {SpreadsheetLimits.MaxRows}.");
                        int columnIndex = 0;
                        foreach (var cellElement in rowElement.Elements(SpreadsheetNs + "Cell"))
                        {
                            var indexAttribute = (string)cellElement.Attribute(SpreadsheetNs + "Index");
                            if (!string.IsNullOrWhiteSpace(indexAttribute))
                            {
                                if (!int.TryParse(indexAttribute, NumberStyles.Integer, CultureInfo.InvariantCulture, out var oneBasedIndex)
                                    || oneBasedIndex <= 0)
                                    throw new InvalidDataException("Invalid SpreadsheetML column index: " + indexAttribute);
                                columnIndex = oneBasedIndex - 1;
                            }
                            if (columnIndex < 0 || columnIndex >= SpreadsheetLimits.MaxColumns)
                                throw new InvalidDataException($"Spreadsheet columns are limited to {SpreadsheetLimits.MaxColumns}.");

                            string formula = (string)cellElement.Attribute(SpreadsheetNs + "Formula") ?? string.Empty;
                            string value = !string.IsNullOrWhiteSpace(formula)
                                ? "=" + formula.TrimStart('=')
                                : cellElement.Element(SpreadsheetNs + "Data")?.Value ?? string.Empty;

                            if (!string.IsNullOrEmpty(value))
                            {
                                totalCells++;
                                if (totalCells > SpreadsheetLimits.MaxMaterializedCells)
                                    throw new InvalidDataException("Spreadsheet cell limit exceeded.");
                                worksheet.SetCell(rowIndex, columnIndex, value);
                            }

                            columnIndex++;
                        }

                        rowIndex++;
                    }

                    workbook.Worksheets.Add(worksheet);
                }

                if (workbook.Worksheets.Count == 0)
                    workbook.Worksheets.Add(new SpreadsheetWorksheet(Path.GetFileNameWithoutExtension(path)));

                return true;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch
            {
                workbook = null;
                return false;
            }
        }

        public static void Save(SpreadsheetWorkbook workbook, string path)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(true),
                Indent = true,
                OmitXmlDeclaration = false
            }))
            {
                writer.WriteStartDocument();
                writer.WriteProcessingInstruction("mso-application", "progid=\"Excel.Sheet\"");
                writer.WriteStartElement("Workbook", SpreadsheetNs.NamespaceName);
                writer.WriteAttributeString("xmlns", "o", null, "urn:schemas-microsoft-com:office:office");
                writer.WriteAttributeString("xmlns", "x", null, "urn:schemas-microsoft-com:office:excel");
                writer.WriteAttributeString("xmlns", "ss", null, SpreadsheetNs.NamespaceName);
                writer.WriteAttributeString("xmlns", "html", null, "http://www.w3.org/TR/REC-html40");

                writer.WriteStartElement("Styles", SpreadsheetNs.NamespaceName);
                writer.WriteStartElement("Style", SpreadsheetNs.NamespaceName);
                writer.WriteAttributeString("ss", "ID", SpreadsheetNs.NamespaceName, "Default");
                writer.WriteAttributeString("ss", "Name", SpreadsheetNs.NamespaceName, "Normal");
                writer.WriteEndElement();
                writer.WriteEndElement();

                for (int i = 0; i < workbook.Worksheets.Count; i++)
                    WriteWorksheet(writer, workbook.Worksheets[i], i + 1);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private static void WriteWorksheet(XmlWriter writer, SpreadsheetWorksheet sheet, int index)
        {
            writer.WriteStartElement("Worksheet", SpreadsheetNs.NamespaceName);
            writer.WriteAttributeString("ss", "Name", SpreadsheetNs.NamespaceName, SafeSheetName(sheet.Name, index));
            writer.WriteStartElement("Table", SpreadsheetNs.NamespaceName);
            writer.WriteAttributeString("ss", "ExpandedColumnCount", SpreadsheetNs.NamespaceName, sheet.UsedColumnCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("ss", "ExpandedRowCount", SpreadsheetNs.NamespaceName, sheet.UsedRowCount.ToString(CultureInfo.InvariantCulture));

            for (int row = 0; row < sheet.UsedRowCount; row++)
            {
                writer.WriteStartElement("Row", SpreadsheetNs.NamespaceName);
                for (int col = 0; col < sheet.UsedColumnCount; col++)
                {
                    string value = sheet.GetCell(row, col);
                    if (string.IsNullOrEmpty(value))
                    {
                        writer.WriteStartElement("Cell", SpreadsheetNs.NamespaceName);
                        writer.WriteEndElement();
                        continue;
                    }

                    writer.WriteStartElement("Cell", SpreadsheetNs.NamespaceName);
                    if (value.Length > 1 && value[0] == '=')
                        writer.WriteAttributeString("ss", "Formula", SpreadsheetNs.NamespaceName, "=" + value.Substring(1));

                    writer.WriteStartElement("Data", SpreadsheetNs.NamespaceName);
                    writer.WriteAttributeString("ss", "Type", SpreadsheetNs.NamespaceName, GetDataType(value));
                    writer.WriteString(value.Length > 1 && value[0] == '=' ? string.Empty : value);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static string GetDataType(string value)
        {
            if (string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase))
                return "Boolean";

            if (value.Length > 1 && value[0] == '=')
                return "String";

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? "Number" : "String";
        }

        private static string SafeSheetName(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Sheet" + index.ToString(CultureInfo.InvariantCulture);

            var invalid = new[] { '[', ']', ':', '*', '?', '/', '\\' };
            foreach (var ch in invalid)
                name = name.Replace(ch, '_');

            return name.Length > 31 ? name.Substring(0, 31) : name;
        }
    }
}
