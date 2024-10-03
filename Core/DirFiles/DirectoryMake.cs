using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Core.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class DirectoryMake
    {
        /// <summary>
        /// Path of directory.
        /// </summary>
        private string Path { get; set; }
        
        /// <summary>
        /// Current Diretory location.
        /// </summary>
        private string CurrentDir { get; set; }
        public DirectoryMake(string path, string currentDirectory) {
        
            Path = path;
            CurrentDir = currentDirectory;
        }

        /// <summary>
        /// Create directory/directories.
        /// </summary>
        public void Create()
        {
            if (Path.Contains(";"))
            {
                var splitPath = Path.Split(';');
                var listDirs = new List<string>();
                foreach (var dir in splitPath)
                {
                    string pathS = FileSystem.SanitizePath(dir, CurrentDir);
                    Directory.CreateDirectory(pathS);
                    listDirs.Add(pathS);
                }
                FileSystem.SuccessWriteLine($"Fallowing directories are created:");
                foreach (var dir in listDirs)
                    FileSystem.SuccessWriteLine(dir);
                return;
            }
            string path = FileSystem.SanitizePath(Path, CurrentDir);
            Directory.CreateDirectory(path);
            FileSystem.SuccessWriteLine($"Directory {path} is created!");
        }
    }
}
