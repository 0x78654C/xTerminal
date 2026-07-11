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
        /// Create Zip folder.
        /// </summary>
        public void Compress()
        {
            string pathDir = FileSystem.SanitizePath(ZipDir, _currentDirectory);//folder to add
            if (!Directory.Exists(pathDir))
            {
                FileSystem.ErrorWriteLine($"Directory does not exist: {pathDir}");
                GlobalVariables.isErrorCommand = true;
                return;
            }
            var count = pathDir.Split('\\').Length;
            var lastDir = pathDir.Split('\\')[count - 1].Length;
            var parentPath = pathDir.Substring(0, pathDir.Length - lastDir);
            string zipPath = Path.Combine(parentPath, ZipName + ".zip");//URL for your ZIP file
            FileSystem.SuccessWriteLine($"Creating Zip file...");
            ZipFile.CreateFromDirectory(pathDir, zipPath, GlobalVariables.compressionLevel, true);
            FileSystem.SuccessWriteLine($"Created Zip file: {zipPath}");
        }

        /// <summary>
        /// Start compress and create achives.
        /// </summary>
        public void Archive()
        {
            string tempDir = null;
            try
            {
                tempDir = CreateTemporaryDirectory();
                string pathFile;

                if (ZipDir.Contains("*"))
                {
                    var splitFiles = ZipDir.Split('*', StringSplitOptions.RemoveEmptyEntries);
                    if (splitFiles.Length == 0)
                        throw new ArgumentException("No archive inputs were supplied.");

                    pathFile = string.Empty;
                    foreach (var file in splitFiles)
                    {
                        pathFile = FileSystem.SanitizePath(file, _currentDirectory);
                        if (!File.Exists(pathFile) && !Directory.Exists(pathFile))
                            throw new FileNotFoundException("Archive input was not found.", pathFile);

                        if (Directory.Exists(pathFile))
                        {
                            var source = new DirectoryInfo(pathFile);
                            if ((source.Attributes & FileAttributes.ReparsePoint) != 0)
                                throw new IOException("Reparse-point directories cannot be archived recursively.");

                            FileDirManager.CopyDirectory(pathFile, Path.Combine(tempDir, source.Name), true);
                        }
                        else
                            File.Copy(pathFile, Path.Combine(tempDir, Path.GetFileName(pathFile)));
                    }

                    var zipPath = Path.Combine(Path.GetDirectoryName(pathFile), ZipName + ".zip");
                    CreateArchive(tempDir, zipPath);
                }
                else
                {
                    pathFile = FileSystem.SanitizePath(ZipDir, _currentDirectory);
                    if (Directory.Exists(pathFile))
                    {
                        DeleteTempDir(tempDir);
                        tempDir = null;
                        Compress();
                        return;
                    }
                    if (!File.Exists(pathFile))
                        throw new FileNotFoundException("Archive input was not found.", pathFile);

                    File.Copy(pathFile, Path.Combine(tempDir, Path.GetFileName(pathFile)));
                    var zipPath = Path.Combine(Path.GetDirectoryName(pathFile), ZipName + ".zip");
                    CreateArchive(tempDir, zipPath);
                }
            }
            finally
            {
                if (tempDir != null)
                    DeleteTempDir(tempDir);
            }
        }

        private static void CreateArchive(string tempDir, string zipPath)
        {
            FileSystem.SuccessWriteLine("Creating Zip file...");
            ZipFile.CreateFromDirectory(tempDir, zipPath, GlobalVariables.compressionLevel, false);
            FileSystem.SuccessWriteLine($"Created Zip file: {zipPath}");
        }

        private static string CreateTemporaryDirectory()
        {
            var directory = Directory.CreateTempSubdirectory("xterminal-zip-");
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to use a reparse point as temporary storage.");
            return directory.FullName;
        }

        /// <summary>
        /// Delete temp dir recursive.
        /// </summary>
        /// <param name="dir"></param>
        private static void DeleteTempDir(string dir)
        {
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                var dirInfo = new DirectoryInfo(dir);
                FileDirManager.RecursiveDeleteDir(dirInfo);
            }
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
                GlobalVariables.isErrorCommand = true;
                return;
            }

            var zipEntries = ZipFile.OpenRead(pathFile).Entries;
            var dataOut = "";
            foreach (var entry in zipEntries)
                dataOut += entry + Environment.NewLine;
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0)
                GlobalVariables.pipeCmdOutput = dataOut;
            else
                Console.WriteLine(dataOut);
        }

        /// <summary>
        /// Unpack Zip file.
        /// </summary>
        public void Decompress()
        {
            string pathFile = FileSystem.SanitizePath(ZipName, _currentDirectory);//folder to add
            if (!File.Exists(pathFile))
            {
                FileSystem.ErrorWriteLine($"Zip file does not exist: {pathFile}");
                GlobalVariables.isErrorCommand = true;
                return;
            }
            FileSystem.SuccessWriteLine($"Extracting.....");
            ZipFile.ExtractToDirectory(pathFile, Path.GetDirectoryName(pathFile) +$"\\{Path.GetFileNameWithoutExtension(pathFile)}");
            FileSystem.SuccessWriteLine($"Extracted Zip file: {Path.GetDirectoryName(pathFile)}\\{Path.GetFileNameWithoutExtension(pathFile)}");
        }
    }
}
