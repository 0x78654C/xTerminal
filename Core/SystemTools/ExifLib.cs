using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Versioning;

/*
 Library for exif information getter.
 */

namespace Core.SystemTools
{
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
    {"0x829A","ExifExposureTime"},
    {"0x829D","ExifFNumber"},
    {"0x8769","ExifIFD"},
    {"0x8773","ICCProfile"},
    {"0x8822","ExifExposureProg"},
    {"0x8824","ExifSpectralSense"},
    {"0x8825","GpsIFD"},
    {"0x8827","ExifISOSpeed"},
    {"0x8828","ExifOECF"},
    {"0x9000","ExifVer"},
    {"0x9003","ExifDTOrig"},
    {"0x9004","ExifDTDigitized"},
    {"0x9101","ExifCompConfig"},
    {"0x9102","ExifCompBPP"},
    {"0x9201","ExifShutterSpeed"},
    {"0x9202","ExifAperture"},
    {"0x9203","ExifBrightness"},
    {"0x9204","ExifExposureBias"},
    {"0x9205","ExifMaxAperture"},
    {"0x9206","ExifSubjectDist"},
    {"0x9207","ExifMeteringMode"},
    {"0x9208","ExifLightSource"},
    {"0x9209","ExifFlash"},
    {"0x920A","ExifFocalLength"},
    {"0x927C","ExifMakerNote"},
    {"0x9286","ExifUserComment"},
    {"0x9290","ExifDTSubsec"},
    {"0x9291","ExifDTOrigSS"},
    {"0x9292","ExifDTDigSS"},
    {"0xA000","ExifFPXVer"},
    {"0xA001","ExifColorSpace"},
    {"0xA002","ExifPixXDim"},
    {"0xA003","ExifPixYDim"},
    {"0xA004","ExifRelatedWav"},
    {"0xA005","ExifInterop"},
    {"0xA20B","ExifFlashEnergy"},
    {"0xA20C","ExifSpatialFR"},
    {"0xA20E","ExifFocalXRes"},
    {"0xA20F","ExifFocalYRes"},
    {"0xA210","ExifFocalResUnit"},
    {"0xA214","ExifSubjectLoc"},
    {"0xA215","ExifExposureIndex"},
    {"0xA217","ExifSensingMethod"},
    {"0xA300","ExifFileSource"},
    {"0xA301","ExifSceneType"},
    {"0xA302","ExifCfaPattern"}
};
        /// <summary>
        /// Print final info.
        /// </summary>
        /// <param name="hexID"></param>
        /// <param name="enc"></param>
        private static void PrintInfo(string hexID, string enc)
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
                            enc = BitConverter.ToUInt16(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 4: // uLong
                            enc = FileSystem.BytesToUInt64(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 5: // uRationl
                            enc = BitConverter.ToUInt64(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 9: // sLong
                            enc = BitConverter.ToInt64(propItem.Value).ToString();
                            PrintInfo(hexID, enc);
                            break;
                        case 10: // sRationl
                            enc = BitConverter.ToUInt64(propItem.Value).ToString();
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
                if (GlobalVariables.isPipeCommand)
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
