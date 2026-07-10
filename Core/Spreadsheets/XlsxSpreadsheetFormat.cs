using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Core.Spreadsheets
{
    internal static class XlsxSpreadsheetFormat
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace WorkbookRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static SpreadsheetWorkbook Load(string path)
        {
            var workbook = new SpreadsheetWorkbook
            {
                SourcePath = path,
                FileKind = SpreadsheetFileKind.Xlsx
            };

            using (var archive = ZipFile.OpenRead(path))
            {
                ValidateArchive(archive);
                var workbookDocument = LoadXml(archive, "xl/workbook.xml");
                var relationships = LoadRelationships(archive, "xl/_rels/workbook.xml.rels", "xl/workbook.xml");
                var sharedStrings = LoadSharedStrings(archive);
                var dateStyles = LoadDateStyleFlags(archive);

                var sheets = workbookDocument.Root
                    .Element(SpreadsheetNs + "sheets")
                    ?.Elements(SpreadsheetNs + "sheet")
                    .ToList() ?? new List<XElement>();
                if (sheets.Count > SpreadsheetLimits.MaxWorksheets)
                    throw new InvalidDataException($"Spreadsheet worksheets are limited to {SpreadsheetLimits.MaxWorksheets}.");

                int totalCells = 0;
                foreach (var sheetElement in sheets)
                {
                    if (workbook.Worksheets.Count >= SpreadsheetLimits.MaxWorksheets)
                        throw new InvalidDataException($"Spreadsheet worksheets are limited to {SpreadsheetLimits.MaxWorksheets}.");
                    string name = (string)sheetElement.Attribute("name") ?? "Sheet";
                    string relationshipId = (string)sheetElement.Attribute(WorkbookRelNs + "id") ?? string.Empty;
                    if (!relationships.TryGetValue(relationshipId, out var targetPath))
                        continue;

                    var worksheet = LoadWorksheet(archive, targetPath, name, sharedStrings, dateStyles, ref totalCells);
                    workbook.Worksheets.Add(worksheet);
                }
            }

            if (workbook.Worksheets.Count == 0)
                workbook.Worksheets.Add(new SpreadsheetWorksheet("Sheet1"));

            return workbook;
        }

        public static void Save(SpreadsheetWorkbook workbook, string path)
        {
            if (workbook.Worksheets.Count == 0)
                workbook.Worksheets.Add(new SpreadsheetWorksheet("Sheet1"));

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                WriteContentTypes(archive, workbook.Worksheets.Count);
                WritePackageRelationships(archive);
                WriteWorkbook(archive, workbook);
                WriteWorkbookRelationships(archive, workbook.Worksheets.Count);
                WriteStyles(archive);
                WriteCoreProperties(archive);
                WriteAppProperties(archive, workbook);

                for (int i = 0; i < workbook.Worksheets.Count; i++)
                    WriteWorksheet(archive, workbook.Worksheets[i], i + 1);
            }
        }

        private static SpreadsheetWorksheet LoadWorksheet(
            ZipArchive archive,
            string sheetPath,
            string sheetName,
            List<string> sharedStrings,
            List<bool> dateStyles,
            ref int totalCells)
        {
            var sheet = new SpreadsheetWorksheet(sheetName);
            var document = LoadXml(archive, sheetPath);
            int fallbackRowIndex = 0;

            foreach (var rowElement in document.Root.Descendants(SpreadsheetNs + "row"))
            {
                int rowIndex = fallbackRowIndex;
                var rowReference = (string)rowElement.Attribute("r");
                if (!string.IsNullOrWhiteSpace(rowReference))
                {
                    if (!int.TryParse(rowReference, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRow)
                        || parsedRow <= 0)
                        throw new InvalidDataException("Invalid XLSX row reference: " + rowReference);
                    rowIndex = parsedRow - 1;
                }
                if (rowIndex < 0 || rowIndex >= SpreadsheetLimits.MaxRows)
                    throw new InvalidDataException($"Spreadsheet rows are limited to {SpreadsheetLimits.MaxRows}.");

                int fallbackColumnIndex = 0;
                foreach (var cellElement in rowElement.Elements(SpreadsheetNs + "c"))
                {
                    int columnIndex = fallbackColumnIndex;
                    var reference = (string)cellElement.Attribute("r");
                    if (!string.IsNullOrWhiteSpace(reference))
                    {
                        if (!TryParseCellReference(reference, out _, out var parsedColumn))
                            throw new InvalidDataException("Invalid XLSX cell reference: " + reference);
                        columnIndex = parsedColumn;
                    }
                    if (columnIndex < 0 || columnIndex >= SpreadsheetLimits.MaxColumns)
                        throw new InvalidDataException($"Spreadsheet columns are limited to {SpreadsheetLimits.MaxColumns}.");

                    string value = ReadCellValue(cellElement, sharedStrings, dateStyles);
                    if (!string.IsNullOrEmpty(value))
                    {
                        totalCells++;
                        if (totalCells > SpreadsheetLimits.MaxMaterializedCells)
                            throw new InvalidDataException("Spreadsheet cell limit exceeded.");
                        sheet.SetCell(rowIndex, columnIndex, value);
                    }

                    fallbackColumnIndex = columnIndex + 1;
                }

                fallbackRowIndex = rowIndex + 1;
            }

            return sheet;
        }

        private static string ReadCellValue(XElement cellElement, List<string> sharedStrings, List<bool> dateStyles)
        {
            var formulaElement = cellElement.Element(SpreadsheetNs + "f");
            if (formulaElement != null && !string.IsNullOrWhiteSpace(formulaElement.Value))
                return "=" + formulaElement.Value;

            string cellType = (string)cellElement.Attribute("t") ?? string.Empty;
            string rawValue = cellElement.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;

            if (cellType == "s")
            {
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                    && index >= 0
                    && index < sharedStrings.Count)
                    return sharedStrings[index];

                return string.Empty;
            }

            if (cellType == "inlineStr")
                return ReadRichText(cellElement.Element(SpreadsheetNs + "is"));

            if (cellType == "b")
                return rawValue == "1" ? "TRUE" : "FALSE";

            if (cellType == "e")
                return string.IsNullOrWhiteSpace(rawValue) ? string.Empty : "#" + rawValue;

            if (TryGetStyleIndex(cellElement, out var styleIndex)
                && styleIndex >= 0
                && styleIndex < dateStyles.Count
                && dateStyles[styleIndex]
                && double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
                return FormatExcelDate(serial);

            return rawValue;
        }

        private static List<string> LoadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();

            var document = LoadXml(entry);
            var strings = new List<string>();
            foreach (var item in document.Root.Elements(SpreadsheetNs + "si"))
                strings.Add(ReadRichText(item));

            return strings;
        }

        private static string ReadRichText(XElement item)
        {
            if (item == null)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var text in item.Descendants(SpreadsheetNs + "t"))
                builder.Append(text.Value);

            return builder.ToString();
        }

        private static List<bool> LoadDateStyleFlags(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/styles.xml");
            if (entry == null)
                return new List<bool>();

            var document = LoadXml(entry);
            var customDateFormats = new HashSet<int>();
            var numFmts = document.Root.Element(SpreadsheetNs + "numFmts");
            if (numFmts != null)
            {
                foreach (var numFmt in numFmts.Elements(SpreadsheetNs + "numFmt"))
                {
                    if (int.TryParse((string)numFmt.Attribute("numFmtId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                        && IsDateFormat((string)numFmt.Attribute("formatCode") ?? string.Empty))
                        customDateFormats.Add(id);
                }
            }

            var flags = new List<bool>();
            var cellXfs = document.Root.Element(SpreadsheetNs + "cellXfs");
            if (cellXfs == null)
                return flags;

            foreach (var xf in cellXfs.Elements(SpreadsheetNs + "xf"))
            {
                int numFmtId = 0;
                int.TryParse((string)xf.Attribute("numFmtId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out numFmtId);
                flags.Add(IsBuiltinDateFormat(numFmtId) || customDateFormats.Contains(numFmtId));
            }

            return flags;
        }

        private static Dictionary<string, string> LoadRelationships(ZipArchive archive, string relationshipPath, string sourcePath)
        {
            var relationships = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entry = archive.GetEntry(relationshipPath);
            if (entry == null)
                return relationships;

            var document = LoadXml(entry);
            foreach (var relationship in document.Root.Elements(PackageRelNs + "Relationship"))
            {
                string id = (string)relationship.Attribute("Id") ?? string.Empty;
                string target = (string)relationship.Attribute("Target") ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(target))
                    relationships[id] = CombinePackagePath(sourcePath, target);
            }

            return relationships;
        }

        private static XDocument LoadXml(ZipArchive archive, string entryName)
        {
            var entry = archive.GetEntry(entryName);
            if (entry == null)
                throw new InvalidDataException("Missing XLSX part: " + entryName);

            return LoadXml(entry);
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = SpreadsheetLimits.MaxArchiveEntryBytes
            }))
                return XDocument.Load(reader, LoadOptions.None);
        }

        private static void ValidateArchive(ZipArchive archive)
        {
            long expandedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                if (entry.Length > SpreadsheetLimits.MaxArchiveEntryBytes)
                    throw new InvalidDataException($"XLSX entry '{entry.FullName}' exceeds the expanded-size limit.");

                expandedBytes = checked(expandedBytes + entry.Length);
                if (expandedBytes > SpreadsheetLimits.MaxArchiveExpandedBytes)
                    throw new InvalidDataException("XLSX archive exceeds the total expanded-size limit.");

                if (entry.Length > 0
                    && (entry.CompressedLength == 0
                        || (double)entry.Length / Math.Max(1, entry.CompressedLength) > SpreadsheetLimits.MaxCompressionRatio))
                    throw new InvalidDataException($"XLSX entry '{entry.FullName}' exceeds the compression-ratio limit.");
            }
        }

        private static string CombinePackagePath(string sourcePath, string targetPath)
        {
            targetPath = targetPath.Replace('\\', '/');
            if (targetPath.StartsWith("/", StringComparison.Ordinal))
                return targetPath.TrimStart('/');

            var parts = new List<string>();
            var sourceParts = sourcePath.Replace('\\', '/').Split('/');
            for (int i = 0; i < sourceParts.Length - 1; i++)
                parts.Add(sourceParts[i]);

            foreach (var part in targetPath.Split('/'))
            {
                if (part == "." || part.Length == 0)
                    continue;
                if (part == "..")
                {
                    if (parts.Count > 0)
                        parts.RemoveAt(parts.Count - 1);
                    continue;
                }

                parts.Add(part);
            }

            return string.Join("/", parts);
        }

        private static void WriteContentTypes(ZipArchive archive, int worksheetCount)
        {
            WriteXmlEntry(archive, "[Content_Types].xml", writer =>
            {
                writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
                writer.WriteStartElement("Default");
                writer.WriteAttributeString("Extension", "rels");
                writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml");
                writer.WriteEndElement();
                writer.WriteStartElement("Default");
                writer.WriteAttributeString("Extension", "xml");
                writer.WriteAttributeString("ContentType", "application/xml");
                writer.WriteEndElement();

                WriteOverride(writer, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                WriteOverride(writer, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
                WriteOverride(writer, "/docProps/core.xml", "application/vnd.openxmlformats-package.core-properties+xml");
                WriteOverride(writer, "/docProps/app.xml", "application/vnd.openxmlformats-officedocument.extended-properties+xml");

                for (int i = 1; i <= worksheetCount; i++)
                    WriteOverride(writer, "/xl/worksheets/sheet" + i.ToString(CultureInfo.InvariantCulture) + ".xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");

                writer.WriteEndElement();
            });
        }

        private static void WritePackageRelationships(ZipArchive archive)
        {
            WriteXmlEntry(archive, "_rels/.rels", writer =>
            {
                writer.WriteStartElement("Relationships", PackageRelNs.NamespaceName);
                WriteRelationship(writer, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "xl/workbook.xml");
                WriteRelationship(writer, "rId2", "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties", "docProps/core.xml");
                WriteRelationship(writer, "rId3", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties", "docProps/app.xml");
                writer.WriteEndElement();
            });
        }

        private static void WriteWorkbook(ZipArchive archive, SpreadsheetWorkbook workbook)
        {
            WriteXmlEntry(archive, "xl/workbook.xml", writer =>
            {
                writer.WriteStartElement("workbook", SpreadsheetNs.NamespaceName);
                writer.WriteAttributeString("xmlns", "r", null, WorkbookRelNs.NamespaceName);
                writer.WriteStartElement("sheets");

                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < workbook.Worksheets.Count; i++)
                {
                    writer.WriteStartElement("sheet");
                    writer.WriteAttributeString("name", GetSafeSheetName(workbook.Worksheets[i].Name, i + 1, usedNames));
                    writer.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("r", "id", WorkbookRelNs.NamespaceName, "rId" + (i + 1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            });
        }

        private static void WriteWorkbookRelationships(ZipArchive archive, int worksheetCount)
        {
            WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", writer =>
            {
                writer.WriteStartElement("Relationships", PackageRelNs.NamespaceName);
                for (int i = 1; i <= worksheetCount; i++)
                {
                    WriteRelationship(
                        writer,
                        "rId" + i.ToString(CultureInfo.InvariantCulture),
                        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet",
                        "worksheets/sheet" + i.ToString(CultureInfo.InvariantCulture) + ".xml");
                }

                WriteRelationship(
                    writer,
                    "rId" + (worksheetCount + 1).ToString(CultureInfo.InvariantCulture),
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles",
                    "styles.xml");

                writer.WriteEndElement();
            });
        }

        private static void WriteStyles(ZipArchive archive)
        {
            WriteXmlEntry(archive, "xl/styles.xml", writer =>
            {
                writer.WriteStartElement("styleSheet", SpreadsheetNs.NamespaceName);
                writer.WriteStartElement("fonts");
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("font");
                writer.WriteStartElement("sz");
                writer.WriteAttributeString("val", "11");
                writer.WriteEndElement();
                writer.WriteStartElement("name");
                writer.WriteAttributeString("val", "Calibri");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("fills");
                writer.WriteAttributeString("count", "2");
                writer.WriteStartElement("fill");
                writer.WriteStartElement("patternFill");
                writer.WriteAttributeString("patternType", "none");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("fill");
                writer.WriteStartElement("patternFill");
                writer.WriteAttributeString("patternType", "gray125");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("borders");
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("border");
                writer.WriteElementString("left", string.Empty);
                writer.WriteElementString("right", string.Empty);
                writer.WriteElementString("top", string.Empty);
                writer.WriteElementString("bottom", string.Empty);
                writer.WriteElementString("diagonal", string.Empty);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("cellStyleXfs");
                writer.WriteAttributeString("count", "1");
                WriteXf(writer);
                writer.WriteEndElement();

                writer.WriteStartElement("cellXfs");
                writer.WriteAttributeString("count", "1");
                WriteXf(writer);
                writer.WriteEndElement();

                writer.WriteStartElement("cellStyles");
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("cellStyle");
                writer.WriteAttributeString("name", "Normal");
                writer.WriteAttributeString("xfId", "0");
                writer.WriteAttributeString("builtinId", "0");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();
            });
        }

        private static void WriteCoreProperties(ZipArchive archive)
        {
            WriteXmlEntry(archive, "docProps/core.xml", writer =>
            {
                writer.WriteStartElement("cp", "coreProperties", "http://schemas.openxmlformats.org/package/2006/metadata/core-properties");
                writer.WriteAttributeString("xmlns", "dc", null, "http://purl.org/dc/elements/1.1/");
                writer.WriteAttributeString("xmlns", "dcterms", null, "http://purl.org/dc/terms/");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteElementString("dc", "creator", "http://purl.org/dc/elements/1.1/", "xTerminal");
                writer.WriteStartElement("dcterms", "created", "http://purl.org/dc/terms/");
                writer.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance", "dcterms:W3CDTF");
                writer.WriteString(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                writer.WriteEndElement();
                writer.WriteEndElement();
            });
        }

        private static void WriteAppProperties(ZipArchive archive, SpreadsheetWorkbook workbook)
        {
            WriteXmlEntry(archive, "docProps/app.xml", writer =>
            {
                writer.WriteStartElement("Properties", "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties");
                writer.WriteAttributeString("xmlns", "vt", null, "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes");
                writer.WriteElementString("Application", "xTerminal");
                writer.WriteElementString("DocSecurity", "0");
                writer.WriteElementString("ScaleCrop", "false");
                writer.WriteStartElement("HeadingPairs");
                writer.WriteStartElement("vt", "vector", null);
                writer.WriteAttributeString("size", "2");
                writer.WriteAttributeString("baseType", "variant");
                writer.WriteStartElement("vt", "variant", null);
                writer.WriteElementString("vt", "lpstr", null, "Worksheets");
                writer.WriteEndElement();
                writer.WriteStartElement("vt", "variant", null);
                writer.WriteElementString("vt", "i4", null, workbook.Worksheets.Count.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            });
        }

        private static void WriteWorksheet(ZipArchive archive, SpreadsheetWorksheet sheet, int sheetNumber)
        {
            WriteXmlEntry(archive, "xl/worksheets/sheet" + sheetNumber.ToString(CultureInfo.InvariantCulture) + ".xml", writer =>
            {
                int rowCount = sheet.UsedRowCount;
                int columnCount = sheet.UsedColumnCount;

                writer.WriteStartElement("worksheet", SpreadsheetNs.NamespaceName);
                writer.WriteAttributeString("xmlns", "r", null, WorkbookRelNs.NamespaceName);
                writer.WriteStartElement("dimension");
                writer.WriteAttributeString("ref", "A1:" + GetCellReference(rowCount - 1, columnCount - 1));
                writer.WriteEndElement();

                writer.WriteStartElement("sheetViews");
                writer.WriteStartElement("sheetView");
                writer.WriteAttributeString("workbookViewId", "0");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("sheetData");
                for (int row = 0; row < rowCount; row++)
                {
                    writer.WriteStartElement("row");
                    writer.WriteAttributeString("r", (row + 1).ToString(CultureInfo.InvariantCulture));

                    for (int col = 0; col < columnCount; col++)
                    {
                        string value = sheet.GetCell(row, col);
                        if (string.IsNullOrEmpty(value))
                            continue;

                        WriteCell(writer, row, col, value);
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            });
        }

        private static void WriteCell(XmlWriter writer, int rowIndex, int columnIndex, string value)
        {
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", GetCellReference(rowIndex, columnIndex));

            if (value.Length > 1 && value[0] == '=')
            {
                writer.WriteElementString("f", value.Substring(1));
            }
            else if (TryGetNumber(value, out var numberText))
            {
                writer.WriteElementString("v", numberText);
            }
            else if (string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteAttributeString("t", "b");
                writer.WriteElementString("v", string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase) ? "1" : "0");
            }
            else
            {
                writer.WriteAttributeString("t", "inlineStr");
                writer.WriteStartElement("is");
                writer.WriteStartElement("t");
                if (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])))
                    writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                writer.WriteString(value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void WriteXmlEntry(ZipArchive archive, string path, Action<XmlWriter> write)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                OmitXmlDeclaration = false
            }))
            {
                writer.WriteStartDocument(true);
                write(writer);
                writer.WriteEndDocument();
            }
        }

        private static void WriteOverride(XmlWriter writer, string partName, string contentType)
        {
            writer.WriteStartElement("Override");
            writer.WriteAttributeString("PartName", partName);
            writer.WriteAttributeString("ContentType", contentType);
            writer.WriteEndElement();
        }

        private static void WriteRelationship(XmlWriter writer, string id, string type, string target)
        {
            writer.WriteStartElement("Relationship", PackageRelNs.NamespaceName);
            writer.WriteAttributeString("Id", id);
            writer.WriteAttributeString("Type", type);
            writer.WriteAttributeString("Target", target);
            writer.WriteEndElement();
        }

        private static void WriteXf(XmlWriter writer)
        {
            writer.WriteStartElement("xf");
            writer.WriteAttributeString("numFmtId", "0");
            writer.WriteAttributeString("fontId", "0");
            writer.WriteAttributeString("fillId", "0");
            writer.WriteAttributeString("borderId", "0");
            writer.WriteAttributeString("xfId", "0");
            writer.WriteEndElement();
        }

        private static bool TryGetStyleIndex(XElement cellElement, out int styleIndex)
        {
            return int.TryParse((string)cellElement.Attribute("s"), NumberStyles.Integer, CultureInfo.InvariantCulture, out styleIndex);
        }

        private static bool TryParseCellReference(string reference, out int rowIndex, out int columnIndex)
        {
            rowIndex = 0;
            columnIndex = 0;
            int index = 0;
            while (index < reference.Length && char.IsLetter(reference[index]))
            {
                long nextColumn = (long)columnIndex * 26 + (char.ToUpperInvariant(reference[index]) - 'A' + 1);
                if (nextColumn > SpreadsheetLimits.MaxColumns)
                    return false;
                columnIndex = (int)nextColumn;
                index++;
            }

            if (columnIndex == 0)
                return false;

            columnIndex--;
            string rowText = reference.Substring(index);
            if (!int.TryParse(rowText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowNumber) || rowNumber <= 0)
                return false;

            rowIndex = rowNumber - 1;
            return true;
        }

        public static string GetCellReference(int rowIndex, int columnIndex)
        {
            return GetColumnName(columnIndex) + (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
        }

        public static string GetColumnName(int columnIndex)
        {
            columnIndex = Math.Max(0, columnIndex);
            var builder = new StringBuilder();
            int value = columnIndex + 1;
            while (value > 0)
            {
                value--;
                builder.Insert(0, (char)('A' + (value % 26)));
                value /= 26;
            }

            return builder.ToString();
        }

        public static bool TryParseReference(string reference, out int rowIndex, out int columnIndex)
        {
            return TryParseCellReference((reference ?? string.Empty).Trim(), out rowIndex, out columnIndex);
        }

        private static string GetSafeSheetName(string requestedName, int sheetNumber, HashSet<string> usedNames)
        {
            var invalid = new HashSet<char>(new[] { '[', ']', ':', '*', '?', '/', '\\' });
            var builder = new StringBuilder();
            foreach (var ch in requestedName ?? string.Empty)
                builder.Append(invalid.Contains(ch) ? '_' : ch);

            var name = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = "Sheet" + sheetNumber.ToString(CultureInfo.InvariantCulture);

            if (name.Length > 31)
                name = name.Substring(0, 31);

            string uniqueName = name;
            int suffix = 2;
            while (usedNames.Contains(uniqueName))
            {
                string suffixText = " " + suffix.ToString(CultureInfo.InvariantCulture);
                int maxBaseLength = Math.Max(1, 31 - suffixText.Length);
                uniqueName = name.Length > maxBaseLength ? name.Substring(0, maxBaseLength) + suffixText : name + suffixText;
                suffix++;
            }

            usedNames.Add(uniqueName);
            return uniqueName;
        }

        private static bool TryGetNumber(string value, out string numberText)
        {
            numberText = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value != value.Trim())
                return false;

            if (value.Length > 1 && value[0] == '0' && char.IsDigit(value[1]))
                return false;

            int digits = value.Count(char.IsDigit);
            if (digits > 15 && value.IndexOf('.') < 0 && value.IndexOf('E') < 0 && value.IndexOf('e') < 0)
                return false;

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return false;

            if (double.IsNaN(number) || double.IsInfinity(number))
                return false;

            numberText = number.ToString("G17", CultureInfo.InvariantCulture);
            return true;
        }

        private static bool IsBuiltinDateFormat(int numFmtId)
        {
            return (numFmtId >= 14 && numFmtId <= 17)
                   || numFmtId == 22
                   || (numFmtId >= 27 && numFmtId <= 36)
                   || (numFmtId >= 45 && numFmtId <= 47)
                   || (numFmtId >= 50 && numFmtId <= 58);
        }

        private static bool IsDateFormat(string formatCode)
        {
            if (string.IsNullOrWhiteSpace(formatCode))
                return false;

            var cleaned = new StringBuilder();
            bool inQuote = false;
            bool inBracket = false;
            foreach (char ch in formatCode.ToLowerInvariant())
            {
                if (ch == '"')
                {
                    inQuote = !inQuote;
                    continue;
                }

                if (ch == '[')
                {
                    inBracket = true;
                    continue;
                }

                if (ch == ']')
                {
                    inBracket = false;
                    continue;
                }

                if (!inQuote && !inBracket)
                    cleaned.Append(ch);
            }

            string code = cleaned.ToString();
            return code.IndexOf('y') >= 0 || code.IndexOf('d') >= 0 || code.IndexOf("mm", StringComparison.Ordinal) >= 0 || code.IndexOf("hh", StringComparison.Ordinal) >= 0;
        }

        private static string FormatExcelDate(double serial)
        {
            try
            {
                var date = DateTime.FromOADate(serial);
                return date.TimeOfDay.TotalSeconds == 0
                    ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                return serial.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
