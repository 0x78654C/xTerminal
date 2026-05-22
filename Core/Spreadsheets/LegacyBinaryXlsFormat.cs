using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Core.Spreadsheets
{
    internal static class LegacyBinaryXlsFormat
    {
        private const ushort Bof = 0x0809;
        private const ushort Eof = 0x000A;
        private const ushort BoundSheet = 0x0085;
        private const ushort Sst = 0x00FC;
        private const ushort Continue = 0x003C;
        private const ushort Number = 0x0203;
        private const ushort Label = 0x0204;
        private const ushort BoolErr = 0x0205;
        private const ushort Formula = 0x0006;
        private const ushort Rk = 0x027E;
        private const ushort MulRk = 0x00BD;
        private const ushort LabelSst = 0x00FD;

        public static SpreadsheetWorkbook Load(string path, byte[] fileBytes)
        {
            var compoundFile = CompoundBinaryFile.Open(fileBytes);
            var workbookBytes = compoundFile.ReadStream("Workbook");
            if (workbookBytes == null || workbookBytes.Length == 0)
                workbookBytes = compoundFile.ReadStream("Book");

            if (workbookBytes == null || workbookBytes.Length == 0)
                throw new InvalidDataException("The .xls workbook stream could not be found.");

            var workbook = new SpreadsheetWorkbook
            {
                SourcePath = path,
                FileKind = SpreadsheetFileKind.LegacyXls
            };

            var sheets = ReadSheetDirectory(workbookBytes);
            var sharedStrings = ReadSharedStrings(workbookBytes);
            if (sheets.Count == 0)
                sheets.Add(new SheetInfo { Name = Path.GetFileNameWithoutExtension(path), Offset = 0 });

            foreach (var info in sheets)
            {
                var sheet = new SpreadsheetWorksheet(info.Name);
                ReadSheetCells(workbookBytes, info.Offset, sheet, sharedStrings);
                workbook.Worksheets.Add(sheet);
            }

            return workbook;
        }

        private static List<SheetInfo> ReadSheetDirectory(byte[] workbookBytes)
        {
            var sheets = new List<SheetInfo>();
            int position = 0;
            while (TryReadRecordHeader(workbookBytes, position, out var id, out var length, out var payloadOffset))
            {
                if (id == BoundSheet && length >= 8)
                {
                    uint sheetOffset = ReadUInt32(workbookBytes, payloadOffset);
                    string name = ReadBoundSheetName(workbookBytes, payloadOffset + 6, length - 6);
                    sheets.Add(new SheetInfo { Name = string.IsNullOrWhiteSpace(name) ? "Sheet" + (sheets.Count + 1).ToString(CultureInfo.InvariantCulture) : name, Offset = (int)sheetOffset });
                }

                position = payloadOffset + length;
            }

            return sheets;
        }

        private static List<string> ReadSharedStrings(byte[] workbookBytes)
        {
            var strings = new List<string>();
            int position = 0;

            while (TryReadRecordHeader(workbookBytes, position, out var id, out var length, out var payloadOffset))
            {
                if (id == Sst)
                {
                    var payload = new List<byte>(length);
                    AddBytes(payload, workbookBytes, payloadOffset, length);

                    int next = payloadOffset + length;
                    while (TryReadRecordHeader(workbookBytes, next, out var nextId, out var nextLength, out var nextPayloadOffset)
                           && nextId == Continue)
                    {
                        AddBytes(payload, workbookBytes, nextPayloadOffset, nextLength);
                        next = nextPayloadOffset + nextLength;
                    }

                    ParseSharedStringPayload(payload.ToArray(), strings);
                    return strings;
                }

                position = payloadOffset + length;
            }

            return strings;
        }

        private static void ReadSheetCells(byte[] workbookBytes, int sheetOffset, SpreadsheetWorksheet sheet, List<string> sharedStrings)
        {
            int position = Math.Max(0, sheetOffset);
            bool seenBof = false;

            while (TryReadRecordHeader(workbookBytes, position, out var id, out var length, out var payloadOffset))
            {
                if (id == Bof)
                    seenBof = true;
                else if (id == Eof && seenBof)
                    break;

                switch (id)
                {
                    case Number:
                        ReadNumberCell(workbookBytes, payloadOffset, length, sheet);
                        break;
                    case Formula:
                        ReadFormulaCell(workbookBytes, payloadOffset, length, sheet);
                        break;
                    case Rk:
                        ReadRkCell(workbookBytes, payloadOffset, length, sheet);
                        break;
                    case MulRk:
                        ReadMulRkCells(workbookBytes, payloadOffset, length, sheet);
                        break;
                    case LabelSst:
                        ReadLabelSstCell(workbookBytes, payloadOffset, length, sheet, sharedStrings);
                        break;
                    case Label:
                        ReadLabelCell(workbookBytes, payloadOffset, length, sheet);
                        break;
                    case BoolErr:
                        ReadBoolErrCell(workbookBytes, payloadOffset, length, sheet);
                        break;
                }

                position = payloadOffset + length;
            }
        }

        private static void ReadNumberCell(byte[] data, int offset, int length, SpreadsheetWorksheet sheet)
        {
            if (length < 14)
                return;

            int row = ReadUInt16(data, offset);
            int col = ReadUInt16(data, offset + 2);
            double value = BitConverter.ToDouble(data, offset + 6);
            sheet.SetCell(row, col, FormatNumber(value));
        }

        private static void ReadFormulaCell(byte[] data, int offset, int length, SpreadsheetWorksheet sheet)
        {
            if (length < 14)
                return;

            int row = ReadUInt16(data, offset);
            int col = ReadUInt16(data, offset + 2);
            byte markerA = data[offset + 12];
            byte markerB = data[offset + 13];
            if (markerA == 0xFF && markerB == 0xFF)
                return;

            double value = BitConverter.ToDouble(data, offset + 6);
            sheet.SetCell(row, col, FormatNumber(value));
        }

        private static void ReadRkCell(byte[] data, int offset, int length, SpreadsheetWorksheet sheet)
        {
            if (length < 10)
                return;

            int row = ReadUInt16(data, offset);
            int col = ReadUInt16(data, offset + 2);
            double value = DecodeRk(ReadUInt32(data, offset + 6));
            sheet.SetCell(row, col, FormatNumber(value));
        }

        private static void ReadMulRkCells(byte[] data, int offset, int length, SpreadsheetWorksheet sheet)
        {
            if (length < 10)
                return;

            int row = ReadUInt16(data, offset);
            int firstCol = ReadUInt16(data, offset + 2);
            int lastCol = ReadUInt16(data, offset + length - 2);
            int cursor = offset + 4;

            for (int col = firstCol; col <= lastCol && cursor + 6 <= offset + length - 2; col++)
            {
                double value = DecodeRk(ReadUInt32(data, cursor + 2));
                sheet.SetCell(row, col, FormatNumber(value));
                cursor += 6;
            }
        }

        private static void ReadLabelSstCell(byte[] data, int offset, int length, SpreadsheetWorksheet sheet, List<string> sharedStrings)
        {
            if (length < 10)
                return;

            int row = ReadUInt16(data, offset);
            int col = ReadUInt16(data, offset + 2);
            int stringIndex = (int)ReadUInt32(data, offset + 6);
            if (stringIndex >= 0 && stringIndex < sharedStrings.Count)
                sheet.SetCell(row, col, sharedStrings[stringIndex]);
        }

        private static void ReadLabelCell(byte[] data, int offset, int length, SpreadsheetWorksheet sheet)
        {
            if (length < 8)
                return;

            int row = ReadUInt16(data, offset);
            int col = ReadUInt16(data, offset + 2);
            int stringOffset = offset + 6;
            if (TryReadUnicodeString(data, ref stringOffset, offset + length, out var value))
                sheet.SetCell(row, col, value);
        }

        private static void ReadBoolErrCell(byte[] data, int offset, int length, SpreadsheetWorksheet sheet)
        {
            if (length < 8)
                return;

            int row = ReadUInt16(data, offset);
            int col = ReadUInt16(data, offset + 2);
            bool isError = data[offset + 7] != 0;
            string value = isError ? "#ERR" : (data[offset + 6] == 0 ? "FALSE" : "TRUE");
            sheet.SetCell(row, col, value);
        }

        private static void ParseSharedStringPayload(byte[] payload, List<string> strings)
        {
            if (payload.Length < 8)
                return;

            int uniqueCount = (int)ReadUInt32(payload, 4);
            int offset = 8;
            for (int i = 0; i < uniqueCount && offset < payload.Length; i++)
            {
                if (!TryReadUnicodeString(payload, ref offset, payload.Length, out var value))
                    break;

                strings.Add(value);
            }
        }

        private static bool TryReadUnicodeString(byte[] data, ref int offset, int endOffset, out string value)
        {
            value = string.Empty;
            if (offset + 3 > endOffset)
                return false;

            int length = ReadUInt16(data, offset);
            offset += 2;
            byte flags = data[offset++];
            bool isUnicode = (flags & 0x01) != 0;
            bool hasExtended = (flags & 0x04) != 0;
            bool hasRichText = (flags & 0x08) != 0;
            int richTextRuns = 0;
            int extendedSize = 0;

            if (hasRichText)
            {
                if (offset + 2 > endOffset)
                    return false;
                richTextRuns = ReadUInt16(data, offset);
                offset += 2;
            }

            if (hasExtended)
            {
                if (offset + 4 > endOffset)
                    return false;
                extendedSize = (int)ReadUInt32(data, offset);
                offset += 4;
            }

            int byteCount = length * (isUnicode ? 2 : 1);
            if (offset + byteCount > endOffset)
                return false;

            value = isUnicode
                ? Encoding.Unicode.GetString(data, offset, byteCount)
                : Encoding.Latin1.GetString(data, offset, byteCount);
            offset += byteCount;

            int skipBytes = richTextRuns * 4 + extendedSize;
            if (offset + skipBytes > endOffset)
                offset = endOffset;
            else
                offset += skipBytes;

            return true;
        }

        private static string ReadBoundSheetName(byte[] data, int offset, int length)
        {
            if (length < 2 || offset + 2 > data.Length)
                return string.Empty;

            int charCount = data[offset];
            byte flags = data[offset + 1];
            bool isUnicode = (flags & 0x01) != 0;
            int textOffset = offset + 2;
            int byteCount = charCount * (isUnicode ? 2 : 1);
            int maxBytes = Math.Min(byteCount, Math.Min(length - 2, data.Length - textOffset));
            if (maxBytes <= 0)
                return string.Empty;

            return isUnicode
                ? Encoding.Unicode.GetString(data, textOffset, maxBytes)
                : Encoding.Latin1.GetString(data, textOffset, maxBytes);
        }

        private static bool TryReadRecordHeader(byte[] data, int position, out ushort id, out int length, out int payloadOffset)
        {
            id = 0;
            length = 0;
            payloadOffset = 0;
            if (position < 0 || position + 4 > data.Length)
                return false;

            id = ReadUInt16(data, position);
            length = ReadUInt16(data, position + 2);
            payloadOffset = position + 4;
            return payloadOffset + length <= data.Length;
        }

        private static double DecodeRk(uint raw)
        {
            double value;
            if ((raw & 0x02U) != 0)
            {
                value = (int)raw >> 2;
            }
            else
            {
                ulong bits = ((ulong)(raw & 0xFFFFFFFCU)) << 32;
                value = BitConverter.Int64BitsToDouble((long)bits);
            }

            if ((raw & 0x01U) != 0)
                value /= 100.0;

            return value;
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("G15", CultureInfo.InvariantCulture);
        }

        private static void AddBytes(List<byte> output, byte[] data, int offset, int count)
        {
            for (int i = 0; i < count && offset + i < data.Length; i++)
                output.Add(data[offset + i]);
        }

        private static ushort ReadUInt16(byte[] data, int offset)
        {
            return BitConverter.ToUInt16(data, offset);
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return BitConverter.ToUInt32(data, offset);
        }

        private sealed class SheetInfo
        {
            public string Name;
            public int Offset;
        }

        private sealed class CompoundBinaryFile
        {
            private const int FreeSector = unchecked((int)0xFFFFFFFF);
            private const int EndOfChain = unchecked((int)0xFFFFFFFE);
            private const int HeaderDifatEntries = 109;
            private readonly byte[] _data;
            private readonly int _sectorSize;
            private readonly int _miniSectorSize;
            private readonly int _miniStreamCutoff;
            private readonly int[] _fat;
            private readonly int[] _miniFat;
            private readonly List<DirectoryEntry> _directoryEntries;
            private readonly byte[] _miniStream;

            private CompoundBinaryFile(
                byte[] data,
                int sectorSize,
                int miniSectorSize,
                int miniStreamCutoff,
                int[] fat,
                int[] miniFat,
                List<DirectoryEntry> directoryEntries,
                byte[] miniStream)
            {
                _data = data;
                _sectorSize = sectorSize;
                _miniSectorSize = miniSectorSize;
                _miniStreamCutoff = miniStreamCutoff;
                _fat = fat;
                _miniFat = miniFat;
                _directoryEntries = directoryEntries;
                _miniStream = miniStream;
            }

            public static CompoundBinaryFile Open(byte[] data)
            {
                if (data == null || data.Length < 512 || !HasSignature(data))
                    throw new InvalidDataException("The file is not a binary .xls compound document.");

                int sectorSize = 1 << ReadUInt16(data, 0x1E);
                int miniSectorSize = 1 << ReadUInt16(data, 0x20);
                int firstDirectorySector = ReadInt32(data, 0x30);
                int miniStreamCutoff = ReadInt32(data, 0x38);
                int firstMiniFatSector = ReadInt32(data, 0x3C);
                int miniFatSectorCount = ReadInt32(data, 0x40);
                int firstDifatSector = ReadInt32(data, 0x44);
                int difatSectorCount = ReadInt32(data, 0x48);
                int fatSectorCount = ReadInt32(data, 0x2C);

                var fatSectorIds = ReadDifat(data, sectorSize, fatSectorCount, firstDifatSector, difatSectorCount);
                var fat = ReadFat(data, sectorSize, fatSectorIds);
                var directoryBytes = ReadRegularStream(data, sectorSize, fat, firstDirectorySector, -1);
                var directoryEntries = ReadDirectoryEntries(directoryBytes);
                var rootEntry = directoryEntries.Find(entry => entry.Type == 5);

                var miniFat = ReadMiniFat(data, sectorSize, fat, firstMiniFatSector, miniFatSectorCount);
                var miniStream = rootEntry == null || rootEntry.StartSector < 0
                    ? new byte[0]
                    : ReadRegularStream(data, sectorSize, fat, rootEntry.StartSector, rootEntry.Size);

                return new CompoundBinaryFile(data, sectorSize, miniSectorSize, miniStreamCutoff, fat, miniFat, directoryEntries, miniStream);
            }

            public byte[] ReadStream(string streamName)
            {
                var entry = _directoryEntries.Find(item =>
                    item.Type == 2 && string.Equals(item.Name, streamName, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                    return null;

                if (entry.Size < _miniStreamCutoff && _miniFat.Length > 0 && _miniStream.Length > 0)
                    return ReadMiniStream(entry.StartSector, entry.Size);

                return ReadRegularStream(_data, _sectorSize, _fat, entry.StartSector, entry.Size);
            }

            private byte[] ReadMiniStream(int startSector, long size)
            {
                if (startSector < 0 || size <= 0)
                    return new byte[0];

                using (var output = new MemoryStream())
                {
                    int sector = startSector;
                    var visited = new HashSet<int>();
                    while (sector >= 0 && sector != EndOfChain && output.Length < size && visited.Add(sector))
                    {
                        long offset = (long)sector * _miniSectorSize;
                        if (offset < 0 || offset >= _miniStream.Length)
                            break;

                        int count = (int)Math.Min(_miniSectorSize, Math.Min(size - output.Length, _miniStream.Length - offset));
                        output.Write(_miniStream, (int)offset, count);

                        if (sector >= _miniFat.Length)
                            break;
                        sector = _miniFat[sector];
                    }

                    return output.ToArray();
                }
            }

            private static bool HasSignature(byte[] data)
            {
                byte[] signature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
                for (int i = 0; i < signature.Length; i++)
                {
                    if (data[i] != signature[i])
                        return false;
                }

                return true;
            }

            private static List<int> ReadDifat(byte[] data, int sectorSize, int fatSectorCount, int firstDifatSector, int difatSectorCount)
            {
                var sectors = new List<int>();
                for (int i = 0; i < HeaderDifatEntries; i++)
                {
                    int value = ReadInt32(data, 0x4C + i * 4);
                    if (value >= 0)
                        sectors.Add(value);
                }

                int sector = firstDifatSector;
                for (int i = 0; i < difatSectorCount && sector >= 0; i++)
                {
                    byte[] bytes = ReadSector(data, sectorSize, sector);
                    int entries = sectorSize / 4 - 1;
                    for (int j = 0; j < entries; j++)
                    {
                        int value = ReadInt32(bytes, j * 4);
                        if (value >= 0)
                            sectors.Add(value);
                    }

                    sector = ReadInt32(bytes, entries * 4);
                }

                if (fatSectorCount > 0 && sectors.Count > fatSectorCount)
                    sectors.RemoveRange(fatSectorCount, sectors.Count - fatSectorCount);

                return sectors;
            }

            private static int[] ReadFat(byte[] data, int sectorSize, List<int> fatSectorIds)
            {
                var fat = new List<int>();
                foreach (int sectorId in fatSectorIds)
                {
                    byte[] sector = ReadSector(data, sectorSize, sectorId);
                    for (int offset = 0; offset + 4 <= sector.Length; offset += 4)
                        fat.Add(ReadInt32(sector, offset));
                }

                return fat.ToArray();
            }

            private static int[] ReadMiniFat(byte[] data, int sectorSize, int[] fat, int firstMiniFatSector, int miniFatSectorCount)
            {
                if (firstMiniFatSector < 0 || miniFatSectorCount <= 0)
                    return new int[0];

                byte[] bytes = ReadRegularStream(data, sectorSize, fat, firstMiniFatSector, (long)miniFatSectorCount * sectorSize);
                var miniFat = new int[bytes.Length / 4];
                for (int i = 0; i < miniFat.Length; i++)
                    miniFat[i] = ReadInt32(bytes, i * 4);

                return miniFat;
            }

            private static byte[] ReadRegularStream(byte[] data, int sectorSize, int[] fat, int startSector, long size)
            {
                if (startSector < 0)
                    return new byte[0];

                using (var output = new MemoryStream())
                {
                    int sector = startSector;
                    var visited = new HashSet<int>();
                    while (sector >= 0 && sector != EndOfChain && visited.Add(sector))
                    {
                        byte[] sectorBytes = ReadSector(data, sectorSize, sector);
                        int count = sectorBytes.Length;
                        if (size >= 0)
                            count = (int)Math.Min(count, size - output.Length);

                        if (count <= 0)
                            break;

                        output.Write(sectorBytes, 0, count);

                        if (size >= 0 && output.Length >= size)
                            break;

                        if (sector >= fat.Length)
                            break;
                        sector = fat[sector];
                    }

                    return output.ToArray();
                }
            }

            private static byte[] ReadSector(byte[] data, int sectorSize, int sector)
            {
                long offset = ((long)sector + 1L) * sectorSize;
                if (sector < 0 || offset < 0 || offset + sectorSize > data.Length)
                    return new byte[sectorSize];

                var bytes = new byte[sectorSize];
                Buffer.BlockCopy(data, (int)offset, bytes, 0, sectorSize);
                return bytes;
            }

            private static List<DirectoryEntry> ReadDirectoryEntries(byte[] bytes)
            {
                var entries = new List<DirectoryEntry>();
                for (int offset = 0; offset + 128 <= bytes.Length; offset += 128)
                {
                    int nameLength = ReadUInt16(bytes, offset + 64);
                    if (nameLength < 2 || nameLength > 64)
                        continue;

                    string name = Encoding.Unicode.GetString(bytes, offset, nameLength - 2);
                    byte type = bytes[offset + 66];
                    int startSector = ReadInt32(bytes, offset + 116);
                    long size = BitConverter.ToInt64(bytes, offset + 120);
                    entries.Add(new DirectoryEntry
                    {
                        Name = name,
                        Type = type,
                        StartSector = startSector,
                        Size = size
                    });
                }

                return entries;
            }

            private static ushort ReadUInt16(byte[] data, int offset)
            {
                return BitConverter.ToUInt16(data, offset);
            }

            private static int ReadInt32(byte[] data, int offset)
            {
                return BitConverter.ToInt32(data, offset);
            }

            private sealed class DirectoryEntry
            {
                public string Name;
                public byte Type;
                public int StartSector;
                public long Size;
            }
        }
    }
}
