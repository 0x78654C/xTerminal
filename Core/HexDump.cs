using System;
using System.Text;
using System.IO;

namespace Core
{
    /*
     Credits to: 
     Link https://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
     Author: Pascal Ganaye
     
     The Code Project Open License (CPOL) 1.02
      THE WORK (AS DEFINED BELOW) IS PROVIDED UNDER THE TERMS OF THIS CODE PROJECT OPEN LICENSE ("LICENSE").
      THE WORK IS PROTECTED BY COPYRIGHT AND/OR OTHER APPLICABLE LAW.
      ANY USE OF THE WORK OTHER THAN AS AUTHORIZED UNDER THIS LICENSE OR COPYRIGHT LAW IS PROHIBITED.
      BY EXERCISING ANY RIGHTS TO THE WORK PROVIDED HEREIN, YOU ACCEPT AND AGREE TO BE BOUND BY THE TERMS OF THIS LICENSE.
      THE AUTHOR GRANTS YOU THE RIGHTS CONTAINED HEREIN IN CONSIDERATION OF YOUR ACCEPTANCE OF SUCH TERMS AND CONDITIONS. 
      IF YOU DO NOT AGREE TO ACCEPT AND BE BOUND BY THE TERMS OF THIS LICENSE, YOU CANNOT MAKE ANY USE OF THE WORK.
     */
    public class HexDump
    {

        private static char[] s_hexChars = "0123456789ABCDEF".ToCharArray();
        /// <summary>
        /// Hex dump
        /// </summary>
        /// <param name="bytes">Bytes input from file</param>
        /// <param name="bytesPerLine">Bytes per line. Default 16</param>
        /// <returns>string </returns>
        public static string Hex(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            int bytesLength = bytes.Length;


            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = s_hexChars[(i >> 28) & 0xF];
                line[1] = s_hexChars[(i >> 24) & 0xF];
                line[2] = s_hexChars[(i >> 20) & 0xF];
                line[3] = s_hexChars[(i >> 16) & 0xF];
                line[4] = s_hexChars[(i >> 12) & 0xF];
                line[5] = s_hexChars[(i >> 8) & 0xF];
                line[6] = s_hexChars[(i >> 4) & 0xF];
                line[7] = s_hexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = s_hexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = s_hexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            return result.ToString();
        }

        /// <summary>
        /// Get hex from file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetHex(string fileName)
        {
            var outHex = "";

            try
            {
                if (File.Exists(fileName))
                {
                    using (var stream = File.OpenRead(fileName))
                    {
                        var size = stream.Length < 50 ? (int)stream.Length : 50;
                        var buffe = new byte[size];
                        var read = stream.Read(buffe, 0, size);
                        outHex=Hex(buffe, size);
                    }
                }
            }
            catch (Exception e)
            {
                outHex = e.Message;
            }
            return outHex;
        }


        /// <summary>
        /// Hex dump bytes only
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="bytesPerLine"></param>
        /// <returns></returns>
        public static string HexBytes(byte[] bytes, int bytesPerLine = 50)
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
