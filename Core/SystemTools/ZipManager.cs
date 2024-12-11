using System;
using System.IO.Compression;
using System.IO;
using System.Runtime.Versioning;

namespace Core.SystemTools
{
    [SupportedOSPlatform("Windows")]
    public class ZipManager
    {
        /// <summary>
        /// Folder path that will be arhived.
        /// </summary>
        public string ZipDir { get; set; }

        /// <summary>
        /// Archive file name.
        /// </summary>
        public string ZipName { get; set; }

        /// <summary>
        /// Current directory read.
        /// </summary>
        private string _currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);

        /// <summary>
        /// cTor for Zip manager.
        /// </summary>
        public ZipManager() { }

        /// <summary>
        /// Create Zip file.
        /// </summary>
        public void Compress()
        {
            string pathDir = FileSystem.SanitizePath(ZipDir,_currentDirectory);//folder to add
            if (!Directory.Exists(pathDir))
            {
                FileSystem.ErrorWriteLine($"Directory does not exist: {pathDir}");
                return;
            }
            string zipPath = Path.Combine(pathDir, ZipName + ".zip");//URL for your ZIP file
            ZipFile.CreateFromDirectory(pathDir, zipPath, CompressionLevel.Fastest,true);
            FileSystem.SuccessWriteLine($"Created Zip file: {zipPath}");
        }


        private void CompressFiles()
        {
            FileSystem
        }

        /// <summary>
        /// List content of zip file.
        /// </summary>
        public void List()
        {
            string pathFile = FileSystem.SanitizePath(ZipName, _currentDirectory);//folder to add
            if (!File.Exists(pathFile))
            {
                FileSystem.ErrorWriteLine($"Archive does not exist: {pathFile}");
                return;
            }

            var zipEntries = ZipFile.OpenRead(pathFile).Entries;
            foreach (var entry in zipEntries)
                Console.WriteLine(entry);
        }

        /// <summary>
        /// Unpack Zip file.
        /// </summary>
        public void Decompress()
        {
            string pathDir = FileSystem.SanitizePath(ZipDir, _currentDirectory);//folder to add
            if (!Directory.Exists(pathDir))
            {
                FileSystem.ErrorWriteLine($"Directory does not exist: {pathDir}");
                return;
            }

            string zipFile = FileSystem.SanitizePath(ZipName, _currentDirectory);//folder to add
            ZipFile.ExtractToDirectory(zipFile, Path.GetDirectoryName(zipFile));
            FileSystem.SuccessWriteLine($"Extracted Zip file: {zipFile}");
        }
    }
}
