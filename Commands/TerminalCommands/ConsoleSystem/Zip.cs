using Core;
using Core.SystemTools;
using System;
using System.IO.Compression;
using System.Runtime.Versioning;

namespace Commands.TerminalCommands.ConsoleSystem
{
    [SupportedOSPlatform("Windows")]
    public class Zip : ITerminalCommand
    {
        public string Name => "zip";
        private static string s_helpMessage = @"Usage of zip command parameters:
    zip <file_/directory_name> -n <name_of_archive> : Creates zip archive with the file/folder mentioned.
    zip <file:dir:dir1:file1> -n <name_of_archive>  : Creates zip archive with the multiple files/folders mentioned.
    zip -list <zip_file_path>                       : Lists the content of the Zip archive file.
    zip -x <zip_file_path>                          : Decompress zip archive.
    zip -c                                          : Sets the compression level (default is Fastest). Example: zip -c s

Compression levels:
o  - Optimal
nc - NoCompression
f  - Fastest
s  - SmallestSize
";
        public void Execute(string arg)
        {
            try
            {
                GlobalVariables.isErrorCommand = false;
                // No parameter.
                if (arg == Name && !GlobalVariables.isPipeCommand)
                {
                    FileSystem.SuccessWriteLine("Use -h for more information!");
                    return;
                }

                arg = arg.Substring(3);

                // Display help message.
                if (arg.Trim() == "-h" && !GlobalVariables.isPipeCommand)
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }
                var zipManager = new ZipManager();
                if (arg.Trim().StartsWith("-c "))
                {
                    var compresionLevel = arg.SplitByText("-c ", 1).Trim();
                    switch (compresionLevel)
                    {
                        case "o":
                            GlobalVariables.compressionLevel = CompressionLevel.Optimal;
                            FileSystem.SuccessWriteLine($"Compression set to: Optimal");
                            break;
                        case "nc":
                            GlobalVariables.compressionLevel = CompressionLevel.NoCompression;
                            FileSystem.SuccessWriteLine($"Compression set to: NoCompression");
                            break;
                        case "f":
                            GlobalVariables.compressionLevel = CompressionLevel.Fastest;
                            FileSystem.SuccessWriteLine($"Compression set to: Fastest");
                            break;
                        case "s":
                            GlobalVariables.compressionLevel = CompressionLevel.SmallestSize;
                            FileSystem.SuccessWriteLine($"Compression set to: SmallestSize");
                            break;
                    }
                    return;
                }

                if (arg.Trim().StartsWith("-list"))
                {
                    var archiveFile = "";
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        archiveFile = GlobalVariables.pipeCmdOutput.Trim();
                    else
                        archiveFile = arg.SplitByText("-list ", 1).Trim();
                    zipManager.ZipName = archiveFile;
                    zipManager.List();
                    return;
                }

                if (arg.Trim().Contains("-n "))
                {
                    var archiveFile = arg.SplitByText("-n ", 1).Trim();
                    var filesToBeArchived = "";
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        filesToBeArchived = GlobalVariables.pipeCmdOutput.Trim();
                    else
                        filesToBeArchived = arg.SplitByText("-n ", 0).Trim();

                    zipManager.ZipName = archiveFile;
                    zipManager.ZipDir = filesToBeArchived;
                    zipManager.Archive();
                    return;
                }

                if (arg.Trim().StartsWith("-x"))
                {
                    var archiveFile = "";
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount == 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        archiveFile = GlobalVariables.pipeCmdOutput.Trim();
                    else
                        archiveFile = arg.SplitByText(" -x ", 1).Trim();
                    zipManager.ZipName = archiveFile;
                    zipManager.Decompress();
                    return;
                }
            }
            catch (Exception ex)
            {
                FileSystem.ErrorWriteLine($"{ex.Message}. Use -h for more information!");
                GlobalVariables.isErrorCommand = true;
            }
        }
    }
}
