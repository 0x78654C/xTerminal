using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class FileSystem
    {
        public static string CurrentLocation = @".\Data\curDir.ini"; //Current Location file path
        public static string EditorPath = @".\Data\cEditor.ini"; //Current editor file path


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
