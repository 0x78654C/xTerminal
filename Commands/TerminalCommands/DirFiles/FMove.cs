﻿using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;


namespace Commands.TerminalCommands.DirFiles
{
    public class FMove : ITerminalCommand
    {
        public string Name => "fmove";

        public void Execute(string arg)
        {

            Console.WriteLine(" ");

            string dlocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ;
            string crcSource = null;
            string crcDestination = null;
            string Source = null;
            string Destination = null;
            string NewPath = null;
            List<string> FilesErrorCopy = new List<string>();
            int countFilesS = 0;
            int countFilesD = 0;
            string[] files;
            string[] dfiles;
            string cmdType = null;
            string codeBase = Assembly.GetExecutingAssembly().GetName().Name;
            double sizeSourceFiles = 0;
            double sizeDestinationFiles = 0;


            Console.WriteLine("\n\r");
            try
            {
                files = Directory.GetFiles(dlocation); ;
                Source = arg.Split(' ')[1];
                cmdType = Source;
                try
                {
                    NewPath = arg.Split(' ')[2];
                }
                catch
                {
                    NewPath = dlocation;
                }


                if (Source.StartsWith("-h"))
                {
                    Console.WriteLine(@"Usage:
 Ex 1: fmove <source_file> <destination_file>
 Ex 2: fmove -ma <destination_directory> -- moves all files in a specific directory
");
                    return;
                }

                //copy all with and without args
                if (Source.StartsWith("-ma"))
                {



                    dfiles = Directory.GetFiles(NewPath);
                    string dFilesList = string.Join("", dfiles);

                    if (NewPath != dlocation && dFilesList.Length > 1)
                    {
                        foreach (var file in dfiles)
                        {

                            if (!file.Contains(") - ") && !file.Contains(codeBase))
                            {

                                int FileCount = 0;
                                int woIndex = 0;
                                int wIndex = 0;
                                int countF = Regex.Matches(file, @"\\").Count;
                                //we get the file name
                                string delilmiterSplitF = file.Split('\\')[countF];

                                //we check if file is already indexed or not
                                if (delilmiterSplitF.StartsWith("(") && delilmiterSplitF.Contains(") - "))
                                {
                                    string fileNameSplit2 = delilmiterSplitF.Split(')')[1];
                                    fileNameSplit2 = fileNameSplit2.Split('-')[1].Trim();
                                    woIndex += Regex.Matches(string.Join(";", dfiles), fileNameSplit2).Count;
                                }
                                else
                                {
                                    wIndex = Regex.Matches(string.Join(";", dfiles), delilmiterSplitF).Count;
                                }
                                FileCount = woIndex + wIndex;
                                FileCount--;


                                Source = dlocation + "\\" + delilmiterSplitF;


                                using (var crc32 = Crc32.Create())
                                {
                                    if (File.Exists(Source))
                                    {
                                        using (var stream = File.OpenRead(Source))
                                        {
                                            var hash = crc32.ComputeHash(stream);
                                            crcSource = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                            Console.WriteLine("Source File: " + Source + " | CRC: " + crcSource + " | Size: " + FileSystem.GetFileSize(Source, false));
                                            sizeSourceFiles += Double.Parse(FileSystem.GetFileSize(Source, true));
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                                        Environment.Exit(-1);
                                    }
                                }

                                Destination = Source;

                                if (!Destination.Contains(":") || !Destination.Contains(@"\"))
                                {
                                    Destination = NewPath + "\\" + Destination;
                                }
                                else
                                {
                                    Destination = NewPath + "\\" + delilmiterSplitF;
                                }


                                //copy module
                                if (File.Exists(Source))
                                {
                                    if (!File.Exists(Destination))
                                    {
                                        File.Copy(Source, Destination);
                                    }
                                    else
                                    {
                                        int index = 0;
                                        int count = Regex.Matches(Destination, @"\\").Count;
                                        string delilmiterSplit = Destination.Split('\\')[count];

                                        if (delilmiterSplit.Contains(") - "))
                                        {
                                            string fileNameSplit = delilmiterSplit.Split(')')[1];
                                            if (fileNameSplit.StartsWith(" - "))
                                            {
                                                string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;
                                                if (!File.Exists(Destination))
                                                {
                                                    File.Copy(Source, Destination);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Destination = NewPath + "\\(" + index + ") - " + delilmiterSplit;
                                            if (File.Exists(Destination))
                                            {
                                                index = 0;
                                                count = Regex.Matches(Destination, @"\\").Count;
                                                delilmiterSplit = Destination.Split('\\')[count];
                                                if (delilmiterSplit.Contains(") - "))
                                                {
                                                    string fileNameSplit = delilmiterSplit.Split(')')[1];
                                                    if (fileNameSplit.StartsWith(" - "))
                                                    {
                                                        string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                        fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                        fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                        Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;
                                                        if (!File.Exists(Destination))
                                                        {
                                                            File.Copy(Source, Destination);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {

                                                File.Copy(Source, Destination);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                                    Environment.Exit(-1);
                                }
                                //-------------------------

                                //Grabing destination file crc
                                using (var crc32 = Crc32.Create())
                                {
                                    using (var stream = File.OpenRead(Destination))
                                    {
                                        var hash = crc32.ComputeHash(stream);
                                        crcDestination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                        Console.WriteLine("Destination File: " + Destination + " | CRC: " + crcDestination + " | Size: " + FileSystem.GetFileSize(Destination, false));
                                        sizeDestinationFiles += Double.Parse(FileSystem.GetFileSize(Destination, true));
                                    }
                                }
                                //--------------------------------


                                if (crcSource == crcDestination)
                                {
                                    File.Delete(Source);
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "CRC match! File was moved OK!" + Environment.NewLine);
                                }
                                else
                                {
                                    File.Delete(Destination);
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "CRC dose not match! File was not moved." + Environment.NewLine);
                                    FilesErrorCopy.Add(Source);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var file in files)
                        {
                            if (!file.Contains(") - "))
                            {
                                int FileCount = 0;
                                int woIndex = 0;
                                int wIndex = 0;
                                int countF = Regex.Matches(file, @"\\").Count;
                                //we get the file name
                                string delilmiterSplitF = file.Split('\\')[countF];

                                //we check if file is already indexed or not
                                if (delilmiterSplitF.StartsWith("(") && delilmiterSplitF.Contains(") - "))
                                {
                                    string fileNameSplit2 = delilmiterSplitF.Split(')')[1];
                                    fileNameSplit2 = fileNameSplit2.Split('-')[1].Trim();
                                    woIndex += Regex.Matches(string.Join(";", files), fileNameSplit2).Count;
                                }
                                else
                                {
                                    wIndex = Regex.Matches(string.Join(";", files), delilmiterSplitF).Count;
                                }
                                FileCount = woIndex + wIndex;
                                FileCount--;
                                Source = file;

                                //check if source is current path
                                if (!Source.Contains(":") || !Source.Contains(@"\"))
                                {
                                    Source = dlocation + "\\" + Source;
                                }

                                using (var crc32 = Crc32.Create())
                                {
                                    if (File.Exists(Source))
                                    {
                                        using (var stream = File.OpenRead(Source))
                                        {
                                            var hash = crc32.ComputeHash(stream);
                                            crcSource = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                            Console.WriteLine("Source File: " + Source + " | CRC: " + crcSource + " | Size: " + FileSystem.GetFileSize(Source, false));
                                            sizeSourceFiles += Double.Parse(FileSystem.GetFileSize(Source, true));
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                                        Environment.Exit(-1);
                                    }
                                }

                                Destination = Source;

                                if (!Destination.Contains(":") || !Destination.Contains(@"\"))
                                {
                                    Destination = NewPath + "\\" + Destination;
                                }
                                else
                                {
                                    Destination = NewPath + "\\" + delilmiterSplitF;
                                }


                                //copy module
                                if (File.Exists(Source))
                                {
                                    if (!File.Exists(Destination))
                                    {

                                        File.Copy(Source, Destination);
                                    }
                                    else
                                    {
                                        int index = 0;
                                        int count = Regex.Matches(Destination, @"\\").Count;
                                        string delilmiterSplit = Destination.Split('\\')[count];

                                        if (delilmiterSplit.Contains(") - "))
                                        {
                                            string fileNameSplit = delilmiterSplit.Split(')')[1];
                                            if (fileNameSplit.StartsWith(" - "))
                                            {
                                                string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;

                                                if (!File.Exists(Destination))
                                                {
                                                    File.Copy(Source, Destination);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Destination = NewPath + "\\(" + index + ") - " + delilmiterSplit;
                                            if (File.Exists(Destination))
                                            {
                                                index = 0;
                                                count = Regex.Matches(Destination, @"\\").Count;
                                                delilmiterSplit = Destination.Split('\\')[count];
                                                if (delilmiterSplit.Contains(") - "))
                                                {
                                                    string fileNameSplit = delilmiterSplit.Split(')')[1];
                                                    if (fileNameSplit.StartsWith(" - "))
                                                    {
                                                        string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                                        fileNameSplit2 = fileNameSplit2.Replace("(", "");
                                                        fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                                        Destination = NewPath + "\\(" + FileCount + ") - " + fileNameSplit;

                                                        if (!File.Exists(Destination))
                                                        {
                                                            File.Copy(Source, Destination);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {

                                                File.Copy(Source, Destination);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                                    Environment.Exit(-1);
                                }
                                //-------------------------

                                //Grabing destination file crc
                                using (var crc32 = Crc32.Create())
                                {
                                    using (var stream = File.OpenRead(Destination))
                                    {
                                        var hash = crc32.ComputeHash(stream);
                                        crcDestination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                        Console.WriteLine("Destination File: " + Destination + " | CRC: " + crcDestination + " | Size: " + FileSystem.GetFileSize(Destination, false));
                                        sizeDestinationFiles += Double.Parse(FileSystem.GetFileSize(Destination, true));
                                    }
                                }
                                //--------------------------------


                                if (crcSource == crcDestination)
                                {
                                    File.Delete(Source);
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "CRC match! File was moved OK!" + Environment.NewLine);
                                }
                                else
                                {
                                    File.Delete(Destination);
                                    FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "CRC dose not match! File was not moved." + Environment.NewLine);
                                    FilesErrorCopy.Add(Source);
                                }
                            }
                        }
                    }
                }
                else
                {

                    if (!Source.Contains(":") || !Source.Contains(@"\"))
                    {
                        Source = dlocation + "\\" + Source;
                    }
                    using (var crc32 = Crc32.Create())
                    {
                        if (File.Exists(Source))
                        {
                            using (var stream = File.OpenRead(Source))
                            {
                                var hash = crc32.ComputeHash(stream);
                                crcSource = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                Console.WriteLine("Source File: " + Source + " | CRC: " + crcSource + " | Size: " + FileSystem.GetFileSize(Source, false));
                                sizeSourceFiles += Double.Parse(FileSystem.GetFileSize(Source, true));
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                            Environment.Exit(-1);
                        }
                    }

                    Destination = arg.Split(' ')[2];

                    if (!Destination.Contains(":") || !Destination.Contains(@"\"))
                    {
                        Destination = dlocation + "\\" + Destination;
                    }

                    //copy module
                    if (File.Exists(Source))
                    {
                        if (!File.Exists(Destination))
                        {

                            File.Copy(Source, Destination);
                        }
                        else
                        {
                            Console.WriteLine($"Destination file '{Destination}' already exist!" + Environment.NewLine);
                            Environment.Exit(-1);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                        Environment.Exit(-1);
                    }
                    //-------------------------
                    //Grabing destination file crc
                    using (var crc32 = Crc32.Create())
                    {
                        using (var stream = File.OpenRead(Destination))
                        {
                            var hash = crc32.ComputeHash(stream);
                            crcDestination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            Console.WriteLine("Destination File: " + Destination + " | CRC: " + crcDestination + " | Size: " + FileSystem.GetFileSize(Destination, false));
                            sizeDestinationFiles += Double.Parse(FileSystem.GetFileSize(Destination, true));
                        }
                    }

                    if (crcSource == crcDestination)
                    {
                        File.Delete(Source);
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Green, "CRC match! File was copied OK!" + Environment.NewLine);
                    }
                    else
                    {
                        File.Delete(Destination);
                        FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "CRC dose not match! File was not moved." + Environment.NewLine);
                        FilesErrorCopy.Add(Source);
                    }
                }
            }
            catch (Exception x)
            {
                if (x.ToString().Contains("is being used by another process"))
                {
                    FileSystem.ErrorWriteLine("File '" + Destination + "' is being used by another process. Terminated!");
                }
                else
                {
                    FileSystem.ErrorWriteLine("Command should look like this: fcopy source_file target_file");
                }

            }
            if (cmdType.StartsWith("-ma"))
            {
                string ErrorCopy = string.Join("\n\r", FilesErrorCopy);
                files = Directory.GetFiles(dlocation);
                countFilesS = files.Count();
                dfiles = Directory.GetFiles(NewPath);
                countFilesD = dfiles.Count();
                double sizeSourceRound = Math.Round(sizeSourceFiles, 2);
                double sizeDestinationRound = Math.Round(sizeDestinationFiles, 2);

                if (!string.IsNullOrWhiteSpace(ErrorCopy))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Red, "List of files not moved. CRC missmatch:\n\r" + ErrorCopy + Environment.NewLine);
                    Console.WriteLine("Total Files Source Directory: " + countFilesS.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                    Console.WriteLine("Total Files Destination Directory: " + countFilesD.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
                }
                else
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Cyan, "\n\r----- All files are moved -----\n\r");
                    Console.WriteLine("Total Files Source Directory: " + countFilesS.ToString() + " | Total Size: " + sizeSourceRound + " MB");
                    Console.WriteLine("Total Files Destination Directory: " + countFilesD.ToString() + " | Total Size: " + sizeDestinationRound + " MB \n\r");
                }
            }
        }
    }
}
