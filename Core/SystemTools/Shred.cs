using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;

/* Overwrite files severtimes with cryptografic data for better delete.*/

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class Shred
    {
        private string _filePath { get; set; }
        private int _writePasses { get; set; }

        /// <summary>
        /// Overwrite a file with cryptografic data serveral times.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="writePasses"></param>
        public Shred(string fileName, int writePasses)
        {
            _filePath = fileName;
            _writePasses = writePasses;
        }

        /// <summary>
        /// Run file shredder.
        /// </summary>
        public void ShredFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, $"File does not exist: {_filePath}");
                    return;
                }

                if (_writePasses == 0)
                    _writePasses = 3;

                File.SetAttributes(_filePath, FileAttributes.Normal);

                var sectors = Math.Ceiling(new FileInfo(_filePath).Length / 512.0);
                var dummyBuffer = new byte[512];
                var cProvier = RandomNumberGenerator.Create();

                var input = new FileStream(_filePath, FileMode.Open);
                for (int currentWrite = 0; currentWrite < _writePasses; currentWrite++)
                {
                    input.Position = 0;
                    Console.WriteLine($"Shred pass: {currentWrite + 1}");

                    for (int writtenSector = 0; writtenSector < sectors; writtenSector++)
                    {
                        cProvier.GetBytes(dummyBuffer);
                        input.Write(dummyBuffer, 0, dummyBuffer.Length);
                    }
                }
                
                input.SetLength(0);
                input.Close();
                var num = RandomNumberGenerator.GetInt32(1,12);
                var dateTime = new DateTime(DateTime.Now.Year - num, num, num+9, num+5, num+num, 0);
                File.SetCreationTime(_filePath, dateTime);
                File.SetLastWriteTime(_filePath, dateTime);
                File.SetLastAccessTime(_filePath, dateTime);
                File.Delete(_filePath);
                FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "File shred completed!");
            }
            catch (UnauthorizedAccessException)
            {
                FileSystem.ErrorWriteLine("Access denied. Try run this as administrator!");
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine(ex.Message);
            }
        }
    }
}
