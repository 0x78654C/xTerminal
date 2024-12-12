﻿using System.IO;
using System.Runtime.Versioning;

namespace Core
{
    [SupportedOSPlatform("windows")]
    public class FileDirManager
    {
        /// <summary>
        /// Copy directories.
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        /// <param name="recursive"></param>
        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                FileSystem.ErrorWriteLine($"Source directory not found: {dir.FullName}");
                return;
            }
            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }


        /// <summary>
        /// Recursive directory delete with file atribute set.
        /// </summary>
        /// <param name="directory"></param>
        public static void RecursiveDeleteDir(DirectoryInfo directory)
        {
            if (!directory.Exists)
            {
                FileSystem.ErrorWriteLine($"Directory '{directory}' does not exist!");
                return;
            }

            foreach (var dir in directory.EnumerateDirectories())
            {
                RecursiveDeleteDir(dir);
            }
            var files = directory.GetFiles();
            foreach (var file in files)
            {
                file.IsReadOnly = false;
                file.Delete();
            }
            directory.Delete();
        }
    }
}
