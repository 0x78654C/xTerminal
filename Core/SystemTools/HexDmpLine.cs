using System;
using System.Text;
using System.IO;

namespace Core.SystemTools
{
    /*
        Get hex dump with bytes limitation.
     */
    public class HexDmpLine
    {
        private string FileName;
        private int Length;

        public HexDmpLine(string fileName, int length)
        {
            FileName = fileName;
            Length = length;
        }

        /// <summary>
        /// Hex dump
        /// </summary>
        /// <param name="fileName">File name for dump hex</param>
        /// <returns>string</returns>
        public string GetHex()
        {
            var outHex = "";
            try
            {
                if (File.Exists(FileName))
                {
                    using (var stream = File.OpenRead(FileName))
                    {
                        var size = stream.Length < Length ? (int)stream.Length : Length;
                        var buffer = new byte[size];
                        var read = stream.Read(buffer, 0, size);
                        outHex = Hex(buffer, size);
                    }
                }
            }
            catch (Exception e)
            {
                outHex = e.Message;
            }
            return outHex;
        }

        private char[] s_hexChars { get; } = "0123456789ABCDEF".ToCharArray();

        /// <summary>
        /// Hex dump
        /// </summary>
        /// <param name="bytes">Bytes input from file</param>
        /// <param name="bytesPerLine">Bytes per line. Default 16</param>
        /// <returns>string</returns>
        private string Hex(byte[] bytes, int bytesPerLine = 50)
        {
            if (bytes is null) return "<null>";
            var bytesLength = bytes.Length;

            var firstCharColumn = 0
                                  + bytesPerLine * 3
                                  + (bytesPerLine - 1) / 8;


            var lineLength = firstCharColumn
                             + bytesPerLine
                             + Environment.NewLine.Length;

            var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            var expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            var result = new StringBuilder(expectedLines * lineLength);

            for (var i = 0; i < bytesLength; i += bytesPerLine)
            {
                var hexColumn = 0;

                for (var j = 0; j < bytesPerLine; j++)
                {
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                    }
                    else
                    {
                        var b = bytes[i + j];
                        line[hexColumn] = s_hexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = s_hexChars[b & 0xF];
                    }

                    hexColumn += 3;
                }

                result.Append(line);
            }
            return result.ToString();
        }
    }
}
