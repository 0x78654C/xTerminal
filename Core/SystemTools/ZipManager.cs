﻿using System;
using System.IO.Compression;
using System.IO;
using System.Runtime.Versioning;
using System.Linq;

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
        /// Create Zip folder.
        /// </summary>
        public void Compress()
        {
            string pathDir = FileSystem.SanitizePath(ZipDir, _currentDirectory);//folder to add
            if (!Directory.Exists(pathDir))
            {
                FileSystem.ErrorWriteLine($"Directory does not exist: {pathDir}");
                return;
            }
            var count = pathDir.Split('\\').Length;
            var lastDir = pathDir.Split('\\')[count - 1].Length;
            var parentPath = pathDir.Substring(0, pathDir.Length - lastDir);
            string zipPath = Path.Combine(parentPath, ZipName + ".zip");//URL for your ZIP file
            ZipFile.CreateFromDirectory(pathDir, zipPath, CompressionLevel.Fastest, true);
            FileSystem.SuccessWriteLine($"Created Zip file: {zipPath}");
        }

        /// <summary>
        /// Start compress and create achives.
        /// </summary>
        public void Archive()
        {
            bool isTempCreated = false;
            var tempDir = "";
            if (ZipDir.Contains(";"))
            {
                var splitFiles = ZipDir.Split(";");
                var pathFile = "";
                foreach (var file in splitFiles)
                {
                    pathFile = FileSystem.SanitizePath(file, _currentDirectory);//folder to add
       
                    if (!isTempCreated)
                    {
                        var getPath = "";
                        if (FileSystem.IsFile(pathFile))
                            getPath = Path.GetDirectoryName(pathFile);
                        else
                        {
                            var len = pathFile.Split('\\').Count();
                            getPath = pathFile.Substring(0, pathFile.Length - pathFile.Split('\\')[len - 1].Length - 1);
                        }
                        tempDir = $"{getPath}\\tmpZip";
                        if (Directory.Exists(tempDir))
                            Directory.Delete(tempDir, true);
                        Directory.CreateDirectory(tempDir);
                        isTempCreated = true;
                    }
                    if (!FileSystem.IsFile(pathFile))
                    {
                        var len = pathFile.Split('\\').Count();
                        var dirName = pathFile.Split('\\')[len - 1];
                        FileDirManager.CopyDirectory(pathFile, $"{tempDir}\\{dirName}",true); ;
                    }
                    else
                       File.Copy(pathFile, $"{tempDir}\\{Path.GetFileName(pathFile)}");
                }

                var zipPath = $"{Path.GetDirectoryName(pathFile)}\\{ZipName}.zip";
                ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Fastest, false);
                FileSystem.SuccessWriteLine($"Created Zip file: {zipPath}");
            }
            else
            {
                var pathFile = FileSystem.SanitizePath(ZipDir, _currentDirectory);//folder to add
                if (!FileSystem.IsFile(pathFile))
                {
                    Compress();
                    return;
                }
                var getPath = Path.GetDirectoryName(pathFile);
                tempDir = $"{getPath}\\tmpZip";
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);
                File.Copy(pathFile, $"{tempDir}\\{Path.GetFileName(pathFile)}");
                var zipPath = $"{Path.GetDirectoryName(pathFile)}\\{ZipName}.zip";
                ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Fastest, false);
                FileSystem.SuccessWriteLine($"Created Zip file: {zipPath}");
            }
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
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
            string pathFile = FileSystem.SanitizePath(ZipDir, _currentDirectory);//folder to add
            if (!File.Exists(pathFile))
            {
                FileSystem.ErrorWriteLine($"Zip file does not exist: {pathFile}");
                return;
            }

            string zipFile = FileSystem.SanitizePath(ZipName, _currentDirectory);//folder to add
            ZipFile.ExtractToDirectory(zipFile, Path.GetDirectoryName(zipFile));
            FileSystem.SuccessWriteLine($"Extracted Zip file: {zipFile}");
        }
    }
}
