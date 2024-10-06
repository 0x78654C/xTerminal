using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

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
        ///  dir1;dir2{sdir1,sdir2};dir3
        /// </summary>
        public void Create(string pathDir)
        {
            if (pathDir.Contains(";"))
            {
                var splitPath = pathDir.Split(';');
                var listDirs = new List<string>();
                foreach (var dir in splitPath)
                {
                    if (dir.Contains("{"))
                    {
                        var splitSub = dir.MiddleStringNoSpace("{", "}").Split(",");
                        var rootDir = dir.Split("{")[0];
                        var pathRoot = FileSystem.SanitizePath(rootDir, CurrentDir);
                        Directory.CreateDirectory(pathRoot);
                        listDirs.Add(pathRoot);
                        foreach (var dirSub in splitSub)
                        {
                            var pathSubWithRoot = $"{pathRoot}\\{dirSub}"; 
                            var pathSub = FileSystem.SanitizePath(pathSubWithRoot, CurrentDir);
                            Directory.CreateDirectory(pathSub);
                            listDirs.Add($"|- {pathSub}");
                        }
                    }
                    else
                    {
                        var pathS = FileSystem.SanitizePath(dir, CurrentDir);
                        Directory.CreateDirectory(pathS);
                        listDirs.Add(pathS);
                    }
                }
                FileSystem.SuccessWriteLine($"Fallowing directories are created:");
                foreach (var dir in listDirs)
                    FileSystem.SuccessWriteLine(dir);
                return;
            }
            var path = FileSystem.SanitizePath(pathDir, CurrentDir);
            Directory.CreateDirectory(path);
            FileSystem.SuccessWriteLine($"Directory {path} is created!");
        }
    }
}
