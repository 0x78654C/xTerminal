using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace Core.DirFiles
{
    [SupportedOSPlatform("Windows")]
    public class DirectoryMake
    {
        /// <summary>
        /// Current Diretory location.
        /// </summary>
        private string CurrentDir { get; set; }
        private string SubDriDelimiter = "";
        public DirectoryMake(string currentDirectory) {

            CurrentDir = currentDirectory;
        }

        /// <summary>
        /// Create directory/directories.
        ///  dir1;dir2{sdir1,sdir2};dir3
        /// </summary>
        public void Create(string pathDir)
        {
            if (pathDir.Contains(";"))
            {
                var splitPath = pathDir.Split(';');
                var listDirs = new List<string>();
                SubDriDelimiter += "-";
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
                            listDirs.Add($"|{SubDriDelimiter} {pathSub}");
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
