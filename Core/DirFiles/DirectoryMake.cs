using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;

namespace Core.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class DirectoryMake
    {
        /// <summary>
        /// Current Diretory location.
        /// </summary>
        private string CurrentDir { get; set; }
        public DirectoryMake(string currentDirectory) {

            CurrentDir = currentDirectory;
        }

        /// <summary>
        /// Create directory/directories and subdirectories.
        /// Example: command dir1;dir2{sdir1,sdir2};dir3
        /// </summary>
        public void Create(string pathDir)
        {
            var listDirs = new List<string>();
            CreateDirectories(pathDir, CurrentDir, listDirs, "");
            FileSystem.SuccessWriteLine("The following directories are created:");
            foreach (var dir in listDirs)
                FileSystem.SuccessWriteLine(dir);
        }

        /// <summary>
        /// Create structure directoris based on pattern
        /// </summary>
        /// <param name="pathDir"></param>
        /// <param name="currentPath"></param>
        /// <param name="listDirs"></param>
        /// <param name="indent"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void CreateDirectories(string pathDir, string currentPath, List<string> listDirs, string indent)
        {
            int index = 0;
            while (index < pathDir.Length)
            {
                // Skip whitespace
                while (index < pathDir.Length && char.IsWhiteSpace(pathDir[index])) index++;

                if (index >= pathDir.Length) break; // End of string

                // Get directory name
                string dirName = ExtractDirectoryName(pathDir, ref index);
                if (!string.IsNullOrEmpty(dirName))
                {
                    // Create the directory
                    string newDirPath = Path.Combine(currentPath, dirName);
                    Directory.CreateDirectory(newDirPath);
                    listDirs.Add($"{indent}{newDirPath}"); // Add the directory path to the list
                }

                // Check for nested directories
                if (index < pathDir.Length && pathDir[index] == '{')
                {
                    int endBraceIndex = FindMatchingBrace(pathDir, index);
                    if (endBraceIndex < 0) throw new InvalidOperationException("Unmatched braces in path.");

                    // Extract nested directories and recursively create them
                    string nestedDirs = pathDir.Substring(index + 1, endBraceIndex - index - 1);
                    CreateDirectories(nestedDirs, Path.Combine(currentPath, dirName), listDirs, $"{indent}   |-- ");
                    index = endBraceIndex + 1; // Move past the closing brace
                }

                // Move past any delimiter (either ':' or ',')
                while (index < pathDir.Length && (pathDir[index] == ':' || pathDir[index] == ',')) index++;
            }
        }

        /// <summary>
        /// Get directory name if 
        /// </summary>
        /// <param name="pathDir"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private string ExtractDirectoryName(string pathDir, ref int index)
        {
            int start = index;
            while (index < pathDir.Length && pathDir[index] != ':' && pathDir[index] != ',' && pathDir[index] != '{')
            {
                index++;
            }
            return pathDir.Substring(start, index - start).Trim();
        }

        /// <summary>
        /// Find matching brace for subdirs.
        /// </summary>
        /// <param name="pathDir"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private int FindMatchingBrace(string pathDir, int startIndex)
        {
            int braceCount = 1; // Start with one brace
            for (int i = startIndex + 1; i < pathDir.Length; i++)
            {
                if (pathDir[i] == '{') braceCount++;
                if (pathDir[i] == '}') braceCount--;

                if (braceCount == 0) return i; // Found the matching brace
            }
            return -1; // No matching brace found
        }
    }
}
