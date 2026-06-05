using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;

/*
 Library for exif information getter.
 */

namespace Core.SystemTools
{
    [SupportedOSPlatform("windows")]
    public sealed class AiDetectionResult
    {
        public bool IsLikelyAiGenerated { get; set; }
        public int Score { get; set; }
        public List<string> Evidence { get; } = new List<string>();

        public string Verdict
        {
            get
            {
                if (Score >= 3) return "LIKELY AI-GENERATED";
                if (Score >= 1) return "POSSIBLY AI-GENERATED";
                return "NO AI METADATA FOUND / UNKNOWN";
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public static class AiFileDetector
    {
        private sealed class Marker
        {
            public string Text { get; }
            public int Weight { get; }
            public string Kind { get; }

            public Marker(string text, int weight, string kind)
            {
                Text = text;
                Weight = weight;
                Kind = kind;
            }
        }

        private static readonly Marker[] Markers = new Marker[]
        {
            // Strong AI tool / generator markers
            new Marker("stable diffusion", 3, "AI tool"),
            new Marker("stable-diffusion", 3, "AI tool"),
            new Marker("stability ai", 3, "AI tool"),
            new Marker("sdxl", 3, "AI model"),
            new Marker("comfyui", 3, "AI tool"),
            new Marker("automatic1111", 3, "AI tool"),
            new Marker("a1111", 3, "AI tool"),
            new Marker("novelai", 3, "AI tool"),
            new Marker("midjourney", 3, "AI tool"),
            new Marker("dall-e", 3, "AI tool"),
            new Marker("dall·e", 3, "AI tool"),
            new Marker("dalle", 3, "AI tool"),
            new Marker("openai", 3, "AI tool"),
            new Marker("adobe firefly", 3, "AI tool"),
            new Marker("firefly", 3, "AI tool"),
            new Marker("bing image creator", 3, "AI tool"),
            new Marker("microsoft designer", 3, "AI tool"),
            new Marker("imagen", 3, "AI tool"),
            new Marker("ideogram", 3, "AI tool"),
            new Marker("leonardo.ai", 3, "AI tool"),
            new Marker("runway", 3, "AI tool"),
            new Marker("recraft", 3, "AI tool"),
            new Marker("invokeai", 3, "AI tool"),
            new Marker("fooocus", 3, "AI tool"),
            new Marker("dreamstudio", 3, "AI tool"),
            new Marker("civitai", 3, "AI tool"),
            new Marker("black forest labs", 3, "AI tool"),
            new Marker("flux", 2, "Possible AI model"),
            new Marker("grok", 2, "Possible AI tool"),

            // Prompt / generation-parameter markers commonly saved by AI image tools
            new Marker("negative prompt", 2, "Generation parameter"),
            new Marker("cfg scale", 2, "Generation parameter"),
            new Marker("sampler:", 2, "Generation parameter"),
            new Marker("seed:", 2, "Generation parameter"),
            new Marker("steps:", 2, "Generation parameter"),
            new Marker("model hash", 2, "Generation parameter"),
            new Marker("model:", 2, "Generation parameter"),
            new Marker("scheduler:", 2, "Generation parameter"),
            new Marker("clip skip", 2, "Generation parameter"),
            new Marker("denoising strength", 2, "Generation parameter"),
            new Marker("hires upscale", 2, "Generation parameter"),
            new Marker("lora:", 2, "Generation parameter"),
            new Marker("loras", 2, "Generation parameter"),
            new Marker("workflow", 1, "Possible AI workflow metadata"),
            new Marker("prompt:", 1, "Possible prompt metadata"),

            // C2PA / content credential / provenance markers
            new Marker("trainedalgorithmicmedia", 4, "AI provenance marker"),
            new Marker("algorithmically generated", 4, "AI provenance marker"),
            new Marker("synthetic media", 3, "AI provenance marker"),
            new Marker("created with ai", 3, "AI provenance marker"),
            new Marker("generated with ai", 3, "AI provenance marker"),
            new Marker("ai-generated", 3, "AI provenance marker"),
            new Marker("ai generated", 3, "AI provenance marker"),
            new Marker("com.adobe.firefly", 4, "AI provenance marker"),
            new Marker("digitalSourceType", 1, "Content credential field"),
            new Marker("c2pa.actions.created", 1, "Content credential field"),
            new Marker("content credentials", 1, "Content credential field")
        };

        public static AiDetectionResult Detect(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            byte[] bytes = File.ReadAllBytes(filePath);
            var result = new AiDetectionResult();
            var seenEvidence = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (MetadataText metadata in ExtractMetadataTexts(bytes, filePath))
            {
                ScanText(metadata.Source, metadata.Text, result, seenEvidence);
            }

            result.IsLikelyAiGenerated = result.Score >= 3;
            return result;
        }

        private sealed class MetadataText
        {
            public string Source { get; }
            public string Text { get; }

            public MetadataText(string source, string text)
            {
                Source = source;
                Text = text ?? string.Empty;
            }
        }

        private static IEnumerable<MetadataText> ExtractMetadataTexts(byte[] bytes, string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Raw strings catch many metadata formats: EXIF, XMP, PNG text chunks, PDF metadata, etc.
            yield return new MetadataText("raw ASCII/UTF-8 strings", ExtractPrintableAsciiStrings(bytes));
            yield return new MetadataText("raw UTF-16 little-endian strings", ExtractUtf16Strings(bytes, bigEndian: false));
            yield return new MetadataText("raw UTF-16 big-endian strings", ExtractUtf16Strings(bytes, bigEndian: true));

            if (LooksLikePng(bytes) || extension == ".png")
            {
                foreach (MetadataText item in ExtractPngTextChunks(bytes))
                    yield return item;
            }

            if (LooksLikeJpeg(bytes) || extension == ".jpg" || extension == ".jpeg")
            {
                foreach (MetadataText item in ExtractJpegMetadataSegments(bytes))
                    yield return item;
            }
        }

        private static void ScanText(string source, string text, AiDetectionResult result, HashSet<string> seenEvidence)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            string lower = text.ToLowerInvariant();

            foreach (Marker marker in Markers)
            {
                string markerLower = marker.Text.ToLowerInvariant();
                int index = lower.IndexOf(markerLower, StringComparison.Ordinal);
                if (index < 0)
                    continue;

                string evidenceKey = source + "|" + marker.Text;
                if (seenEvidence.Contains(evidenceKey))
                    continue;

                seenEvidence.Add(evidenceKey);
                result.Score += marker.Weight;

                string snippet = MakeSnippet(text, index, marker.Text.Length, 130);
                result.Evidence.Add($"{marker.Kind}: found '{marker.Text}' in {source}. Snippet: \"{snippet}\"");
            }
        }

        private static string MakeSnippet(string text, int index, int length, int maxLength)
        {
            int start = Math.Max(0, index - 45);
            int end = Math.Min(text.Length, index + length + 45);
            string snippet = text.Substring(start, end - start);

            snippet = snippet.Replace('\r', ' ')
                             .Replace('\n', ' ')
                             .Replace('\t', ' ')
                             .Replace("\0", " ");

            while (snippet.Contains("  "))
                snippet = snippet.Replace("  ", " ");

            if (snippet.Length > maxLength)
                snippet = snippet.Substring(0, maxLength - 1) + "…";

            return snippet.Trim();
        }

        private static bool LooksLikePng(byte[] bytes)
        {
            return bytes.Length >= 8 &&
                   bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                   bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
        }

        private static bool LooksLikeJpeg(byte[] bytes)
        {
            return bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8;
        }

        private static IEnumerable<MetadataText> ExtractPngTextChunks(byte[] bytes)
        {
            if (!LooksLikePng(bytes))
                yield break;

            int pos = 8;
            while (pos + 12 <= bytes.Length)
            {
                uint length = ReadUInt32BigEndian(bytes, pos);
                pos += 4;

                if (pos + 4 > bytes.Length)
                    yield break;

                string type = Encoding.ASCII.GetString(bytes, pos, 4);
                pos += 4;

                if (length > int.MaxValue || pos + length + 4 > bytes.Length)
                    yield break;

                int dataStart = pos;
                int dataLength = (int)length;

                if (type == "tEXt" || type == "iTXt")
                {
                    string text = Encoding.UTF8.GetString(bytes, dataStart, dataLength).Replace('\0', ' ');
                    yield return new MetadataText("PNG " + type + " chunk", text);
                }
                else if (type == "zTXt")
                {
                    // zTXt is compressed. We do not decompress it here to keep this file dependency-free.
                    string text = Encoding.UTF8.GetString(bytes, dataStart, dataLength).Replace('\0', ' ');
                    yield return new MetadataText("PNG zTXt chunk name/compressed bytes", text);
                }
                else if (type == "eXIf" || type == "iCCP")
                {
                    string text = ExtractPrintableAsciiStrings(SubArray(bytes, dataStart, dataLength));
                    yield return new MetadataText("PNG " + type + " chunk", text);
                }

                pos += dataLength;
                pos += 4; // CRC

                if (type == "IEND")
                    yield break;
            }
        }

        private static IEnumerable<MetadataText> ExtractJpegMetadataSegments(byte[] bytes)
        {
            if (!LooksLikeJpeg(bytes))
                yield break;

            int pos = 2;
            while (pos + 4 <= bytes.Length)
            {
                if (bytes[pos] != 0xFF)
                {
                    pos++;
                    continue;
                }

                while (pos < bytes.Length && bytes[pos] == 0xFF)
                    pos++;

                if (pos >= bytes.Length)
                    yield break;

                byte marker = bytes[pos++];

                // Start of Scan or End of Image: compressed image data starts/ends here.
                if (marker == 0xDA || marker == 0xD9)
                    yield break;

                // Markers without length field.
                if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                    continue;

                if (pos + 2 > bytes.Length)
                    yield break;

                int segmentLength = (bytes[pos] << 8) | bytes[pos + 1];
                pos += 2;

                if (segmentLength < 2)
                    yield break;

                int dataLength = segmentLength - 2;
                if (pos + dataLength > bytes.Length)
                    yield break;

                bool isMetadataSegment =
                    marker == 0xE1 || // APP1: EXIF/XMP
                    marker == 0xED || // APP13: Photoshop/IPTC
                    marker == 0xFE;   // COM: comment

                if (isMetadataSegment)
                {
                    byte[] segment = SubArray(bytes, pos, dataLength);
                    string text = ExtractPrintableAsciiStrings(segment);
                    yield return new MetadataText("JPEG marker 0x" + marker.ToString("X2"), text);
                }

                pos += dataLength;
            }
        }

        private static uint ReadUInt32BigEndian(byte[] data, int index)
        {
            return ((uint)data[index] << 24) |
                   ((uint)data[index + 1] << 16) |
                   ((uint)data[index + 2] << 8) |
                   data[index + 3];
        }

        private static byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] output = new byte[length];
            Buffer.BlockCopy(data, index, output, 0, length);
            return output;
        }

        private static string ExtractPrintableAsciiStrings(byte[] bytes, int minLength = 4)
        {
            var output = new StringBuilder();
            var current = new StringBuilder();

            foreach (byte b in bytes)
            {
                bool printable = b == 9 || b == 10 || b == 13 || (b >= 32 && b <= 126);

                if (printable)
                {
                    current.Append((char)b);
                }
                else
                {
                    FlushString(current, output, minLength);
                }
            }

            FlushString(current, output, minLength);
            return output.ToString();
        }

        private static string ExtractUtf16Strings(byte[] bytes, bool bigEndian, int minLength = 4)
        {
            var output = new StringBuilder();
            var current = new StringBuilder();

            for (int i = 0; i + 1 < bytes.Length; i += 2)
            {
                ushort value;
                if (bigEndian)
                    value = (ushort)((bytes[i] << 8) | bytes[i + 1]);
                else
                    value = (ushort)((bytes[i + 1] << 8) | bytes[i]);

                char c = (char)value;
                bool printable = c == '\t' || c == '\n' || c == '\r' || (c >= 32 && c <= 126);

                if (printable)
                {
                    current.Append(c);
                }
                else
                {
                    FlushString(current, output, minLength);
                }
            }

            FlushString(current, output, minLength);
            return output.ToString();
        }

        private static void FlushString(StringBuilder current, StringBuilder output, int minLength)
        {
            if (current.Length >= minLength)
            {
                output.AppendLine(current.ToString());
            }

            current.Clear();
        }
    }

    [SupportedOSPlatform("windows")]
    public class ExifLib
    {
        private static string s_outData = string.Empty;
        private static Dictionary<string, string> s_offset = new Dictionary<string, string>
{
    {"0x0000","GpsVer"},
    {"0x0001","GpsLatitudeRef"},
    {"0x0002","GpsLatitude"},
    {"0x0003","GpsLongitudeRef"},
    {"0x0004","GpsLongitude"},
    {"0x0005","GpsAltitudeRef"},
    {"0x0006","GpsAltitude"},
    {"0x0007","GpsGpsTime"},
    {"0x0008","GpsGpsSatellites"},
    {"0x0009","GpsGpsStatus"},
    {"0x000A","GpsGpsMeasureMode"},
    {"0x000B","GpsGpsDop"},
    {"0x000C","GpsSpeedRef"},
    {"0x000D","GpsSpeed"},
    {"0x000E","GpsTrackRef"},
    {"0x000F","GpsTrack"},
    {"0x0010","GpsImgDirRef"},
    {"0x0011","GpsImgDir"},
    {"0x0012","GpsMapDatum"},
    {"0x0013","GpsDestLatRef"},
    {"0x0014","GpsDestLat"},
    {"0x0015","GpsDestLongRef"},
    {"0x0016","GpsDestLong"},
    {"0x0017","GpsDestBearRef"},
    {"0x0018","GpsDestBear"},
    {"0x0019","GpsDestDistRef"},
    {"0x001A","GpsDestDist"},
    {"0x001D","GpsDateStamp"},
    {"0x001F","GpsHPositioningError"},
    {"0x00FE","NewSubfileType"},
    {"0x00FF","SubfileType"},
    {"0x0100","ImageWidth"},
    {"0x0101","ImageHeight"},
    {"0x0102","BitsPerSample"},
    {"0x0103","Compression"},
    {"0x0106","PhotometricInterp"},
    {"0x0107","ThreshHolding"},
    {"0x0108","CellWidth"},
    {"0x0109","CellHeight"},
    {"0x010A","FillOrder"},
    {"0x010D","DocumentName"},
    {"0x010E","ImageDescription"},
    {"0x010F","EquipMake"},
    {"0x0110","EquipModel"},
    {"0x0111","StripOffsets"},
    {"0x0112","Orientation"},
    {"0x0115","SamplesPerPixel"},
    {"0x0116","RowsPerStrip"},
    {"0x0117","StripBytesCount"},
    {"0x0118","MinSampleValue"},
    {"0x0119","MaxSampleValue"},
    {"0x011A","XResolution"},
    {"0x011B","YResolution"},
    {"0x011C","PlanarConfig"},
    {"0x011D","PageName"},
    {"0x011E","XPosition"},
    {"0x011F","YPosition"},
    {"0x0120","FreeOffset"},
    {"0x0121","FreeByteCounts"},
    {"0x0122","GrayResponseUnit"},
    {"0x0123","GrayResponseCurve"},
    {"0x0124","T4Option"},
    {"0x0125","T6Option"},
    {"0x0128","ResolutionUnit"},
    {"0x0129","PageNumber"},
    {"0x012D","TransferFunction"},
    {"0x0131","SoftwareUsed"},
    {"0x0132","DateTime"},
    {"0x013B","Artist"},
    {"0x013C","HostComputer"},
    {"0x013D","Predictor"},
    {"0x013E","WhitePoint"},
    {"0x013F","PrimaryChromaticities"},
    {"0x0140","ColorMap"},
    {"0x0141","HalftoneHints"},
    {"0x0142","TileWidth"},
    {"0x0143","TileLength"},
    {"0x0144","TileOffset"},
    {"0x0145","TileByteCounts"},
    {"0x014C","InkSet"},
    {"0x014D","InkNames"},
    {"0x014E","NumberOfInks"},
    {"0x0150","DotRange"},
    {"0x0151","TargetPrinter"},
    {"0x0152","ExtraSamples"},
    {"0x0153","SampleFormat"},
    {"0x0154","SMinSampleValue"},
    {"0x0155","SMaxSampleValue"},
    {"0x0156","TransferRange"},
    {"0x0200","JPEGProc"},
    {"0x0201","JPEGInterFormat"},
    {"0x0202","JPEGInterLength"},
    {"0x0203","JPEGRestartInterval"},
    {"0x0205","JPEGLosslessPredictors"},
    {"0x0206","JPEGPointTransforms"},
    {"0x0207","JPEGQTables"},
    {"0x0208","JPEGDCTables"},
    {"0x0209","JPEGACTables"},
    {"0x0211","YCbCrCoefficients"},
    {"0x0212","YCbCrSubsampling"},
    {"0x0213","YCbCrPositioning"},
    {"0x02BC","Inf"},
    {"0x0214","REFBlackWhite"},
    {"0x0301","Gamma"},
    {"0x0302","ICCProfileDescriptor"},
    {"0x0303","SRGBRenderingIntent"},
    {"0x0320","ImageTitle"},
    {"0x5001","ResolutionXUnit"},
    {"0x5002","ResolutionYUnit"},
    {"0x5003","ResolutionXLengthUnit"},
    {"0x5004","ResolutionYLengthUnit"},
    {"0x5005","PrintFlags"},
    {"0x5006","PrintFlagsVersion"},
    {"0x5007","PrintFlagsCrop"},
    {"0x5008","PrintFlagsBleedWidth"},
    {"0x5009","PrintFlagsBleedWidthScale"},
    {"0x500A","HalftoneLPI"},
    {"0x500B","HalftoneLPIUnit"},
    {"0x500C","HalftoneDegree"},
    {"0x500D","HalftoneShape"},
    {"0x500E","HalftoneMisc"},
    {"0x500F","HalftoneScreen"},
    {"0x5010","JPEGQuality"},
    {"0x5011","GridSize"},
    {"0x5012","ThumbnailFormat"},
    {"0x5013","ThumbnailWidth"},
    {"0x5014","ThumbnailHeight"},
    {"0x5015","ThumbnailColorDepth"},
    {"0x5016","ThumbnailPlanes"},
    {"0x5017","ThumbnailRawBytes"},
    {"0x5018","ThumbnailSize"},
    {"0x5019","ThumbnailCompressedSize"},
    {"0x501A","ColorTransferFunction"},
    {"0x501B","ThumbnailData"},
    {"0x5020","ThumbnailImageWidth"},
    {"0x5021","ThumbnailImageHeight"},
    {"0x5022","ThumbnailBitsPerSample"},
    {"0x5023","ThumbnailCompression"},
    {"0x5024","ThumbnailPhotometricInterp"},
    {"0x5025","ThumbnailImageDescription"},
    {"0x5026","ThumbnailEquipMake"},
    {"0x5027","ThumbnailEquipModel"},
    {"0x5028","ThumbnailStripOffsets"},
    {"0x5029","ThumbnailOrientation"},
    {"0x502A","ThumbnailSamplesPerPixel"},
    {"0x502B","ThumbnailRowsPerStrip"},
    {"0x502C","ThumbnailStripBytesCount"},
    {"0x502D","ThumbnailResolutionX"},
    {"0x502E","ThumbnailResolutionY"},
    {"0x502F","ThumbnailPlanarConfig"},
    {"0x5030","ThumbnailResolutionUnit"},
    {"0x5031","ThumbnailTransferFunction"},
    {"0x5032","ThumbnailSoftwareUsed"},
    {"0x5033","ThumbnailDateTime"},
    {"0x5034","ThumbnailArtist"},
    {"0x5035","ThumbnailWhitePoint"},
    {"0x5036","ThumbnailPrimaryChromaticities"},
    {"0x5037","ThumbnailYCbCrCoefficients"},
    {"0x5038","ThumbnailYCbCrSubsampling"},
    {"0x5039","ThumbnailYCbCrPositioning"},
    {"0x503A","ThumbnailRefBlackWhite"},
    {"0x503B","ThumbnailCopyRight"},
    {"0x5090","LuminanceTable"},
    {"0x5091","ChrominanceTable"},
    {"0x5100","FrameDelay"},
    {"0x5101","LoopCount"},
    {"0x5102","GlobalPalette"},
    {"0x5103","IndexBackground"},
    {"0x5104","IndexTransparent"},
    {"0x5110","PixelUnit"},
    {"0x5111","PixelPerUnitX"},
    {"0x5112","PixelPerUnitY"},
    {"0x5113","PaletteHistogram"},
    {"0x8298","Copyright"},
    {"0x829A","ExposureTime"},
    {"0x829D","FNumber"},
    {"0x8769","IFD"},
    {"0x8773","ICCProfile"},
    {"0x8822","ExposureProg"},
    {"0x8824","SpectralSense"},
    {"0x8825","GpsIFD"},
    {"0x8827","ISOSpeed"},
    {"0x8828","OECF"},
    {"0x9000","Ver"},
    {"0x9003","DateTimeOrig"},
    {"0x9004","DateTimeDigitized"},
    {"0x9010","OffsetTime"},
    {"0x9011","OffsetTimeOriginal"},
    {"0x9012","OffsetTimeDigitized"},
    {"0x9101","CompConfig"},
    {"0x9102","CompBPP"},
    {"0x9201","ShutterSpeed"},
    {"0x9202","Aperture"},
    {"0x9203","Brightness"},
    {"0x9204","ExposureBias"},
    {"0x9205","MaxAperture"},
    {"0x9206","SubjectDist"},
    {"0x9207","MeteringMode"},
    {"0x9208","LightSource"},
    {"0x9209","Flash"},
    {"0x920A","FocalLength"},
    {"0x9214","SubjectArea"},
    {"0x927C","MakerNote"},
    {"0x9286","UserComment"},
    {"0x9290","DTSubsec"},
    {"0x9291","DTOrigSS"},
    {"0x9292","DTDigSS"},
    {"0xA000","FPXVer"},
    {"0xA001","ColorSpace"},
    {"0xA002","PixXDim"},
    {"0xA003","PixYDim"},
    {"0xA004","RelatedWav"},
    {"0xA005","Interop"},
    {"0xA20B","FlashEnergy"},
    {"0xA20C","SpatialFR"},
    {"0xA20E","FocalXRes"},
    {"0xA20F","FocalYRes"},
    {"0xA210","FocalResUnit"},
    {"0xA214","SubjectLoc"},
    {"0xA215","ExposureIndex"},
    {"0xA217","SensingMethod"},
    {"0xA300","FileSource"},
    {"0xA301","SceneType"},
    {"0xA302","CfaPattern"},
    {"0xA402","ExposureMode"},
    {"0xA403","WhiteBalance"},
    {"0xA405","FocalLengthIn35mmFilm"},
    {"0xA406","SceneCaptureType"},
    {"0xA432","LensSpecification"},
    {"0xA433","LensMake"},
    {"0xA434","LensModel"}
};

        /// <summary>
        /// Print final info.
        /// </summary>
        /// <param name="hexID"></param>
        /// <param name="enc"></param>
        private static void PrintInfo(string hexID, string enc, string filePath="")
        {

            if (s_offset.ContainsKey(hexID))
            {
                string value = s_offset[hexID];
                s_outData += String.Format("{0}{1}", $"{value}:".PadRight(30, ' '), $"{enc}\n");
            }
            else
            {
                s_outData += String.Format("{0}{1}", "Undefined:".PadRight(30, ' '), $"{enc}\n");
            }
        }

        /// <summary>
        /// Get Exif information from image.
        /// </summary>
        /// <param name="pathJPEGFile"></param>
        public static void GetExifInfo(string pathJPEGFile)
        {

            try
            {
                Image theImage = new Bitmap(pathJPEGFile);
                PropertyItem[] propItems = theImage.PropertyItems;

                foreach (PropertyItem propItem in propItems)
                {

                    var hexID = String.Format("0x{0:X4}", propItem.Id);
                    var typeE = propItem.Type;
                    var enc = string.Empty;
                    switch (typeE)
                    {
                        case 2: // Ascii
                            enc = Encoding.UTF8.GetString(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 3: // uShrot
                            enc = FileSystem.UShortConversionSlice(propItem.Value,2);
                            PrintInfo(hexID, enc);
                            break;
                        case 4: // uLong
                            enc = FileSystem.BytesToUInt64(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 5: // uRationl
                            enc = FileSystem.UInt32BigEndianConversionSlice(propItem.Value,4);
                            PrintInfo(hexID, enc);
                            break;
                        case 9: // sLong
                            enc = BitConverter.ToInt64(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 10: // sRationl
                            enc = FileSystem.UInt32BigEndianConversionSlice(propItem.Value,4);
                            PrintInfo(hexID, enc);
                            break;
                        case 11: // Float (just for some types of format)
                            enc = BitConverter.ToSingle(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 12: // Double (just for some types of format)
                            enc = BitConverter.ToDouble(propItem.Value, 0).ToString();
                            PrintInfo(hexID, enc);
                            break;
                    }
                }
                AiDetectionResult result = AiFileDetector.Detect(pathJPEGFile);
                if (result.IsLikelyAiGenerated)
                {
                    var aiData = @$"Verdict: {result.Verdict}
Score: {result.Score}
Likely AI generated: {result.IsLikelyAiGenerated}";
                    if (result.Evidence.Count > 0)
                    {
                        aiData += "\nEvidence:";
                        foreach (string evidence in result.Evidence)
                        {
                            aiData += "\n- " + evidence;
                        }
                    }
                    s_outData += aiData + "\n";
                }
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    GlobalVariables.pipeCmdOutput = s_outData;
                else if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == GlobalVariables.pipeCmdCountTemp)
                    GlobalVariables.pipeCmdOutput = s_outData;
                else if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                    GlobalVariables.pipeCmdOutput = s_outData;
                else
                Console.WriteLine(s_outData);
                s_outData = string.Empty;
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e);
            }
        }
    }
}
