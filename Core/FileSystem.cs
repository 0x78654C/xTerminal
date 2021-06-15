using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Core
{
    public class FileSystem
    {
        public static string CurrentLocation = @".\Data\curDir.ini"; //Current Location file path
        public static string EditorPath = @".\Data\cEditor.ini"; //Current editor file path
        private static string[] sizes = { "B", "KB", "MB", "GB", "TB" };


        /// <summary>
        /// Get the size of a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="sizeType">Options: 1 - Bytes |
        /// 2 - Kb |
        /// 3 - Mb |
        /// 4 - Gb |
        /// 5 - Tb | </param>
        /// <returns></returns>
        public static string GetFileSize(string fileName)
        {
            double len = new FileInfo(fileName).Length;
            int order = 0;
            while(len >= 1024 && order < sizes.Length -1)
            {
                order++;
                len = len / 1024;
            }
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        public static double GetFixedFileSize(string fileName)
        {
            double len = new FileInfo(fileName).Length;
            string sLen = String.Format("{0:0.##}", len);
            double fLen = Convert.ToDouble(sLen);
            for(int i = 0; i < 2; i++)
            {
                fLen = fLen / 1024;
            }
            return fLen;
        }

        /// <summary>
        /// Change color of a specific line
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public static void ColorConsoleLine(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
