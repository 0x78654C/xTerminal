using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core;

namespace FCopy
{
    /* Copy file with MD5 checksum*/
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine(" ");
            //reading current location(for test no, after i make dynamic)
            string dlocation = File.ReadAllText(FileSystem.CurrentLocation);
            string md5Source = null;
            string md5Destination = null;
            string Source = null;
            string Destination = null;

            try
            {

                Source = args[0];
                if (!Source.StartsWith("-a"))
                {



                    if (!Source.Contains(":") || !Source.Contains(@"\"))
                    {

                        Source = dlocation + "\\" + Source;

                    }
                    using (var md5 = MD5.Create())
                    {
                        if (File.Exists(Source))
                        {
                            using (var stream = File.OpenRead(Source))
                            {
                                var hash = md5.ComputeHash(stream);
                                md5Source = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                            }
                        }
                        else
                        {
                            Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                            Environment.Exit(-1);
                        }
                    }




                    Destination = args[1];
                    if (!Destination.Contains(":") || !Destination.Contains(@"\"))
                    {
                        Destination = dlocation + "\\" + Destination;
                    }

                    //copy module
                    if (File.Exists(Source))
                    {
                        if (!File.Exists(Destination))
                        {
                            Console.WriteLine("Copy file...\r\n");
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
                    //Grabing destination file md5
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(Destination))
                        {
                            var hash = md5.ComputeHash(stream);
                            md5Destination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                        }
                    }



                    Thread.Sleep(1000);
                    if (md5Source == md5Destination)
                    {
                        Console.WriteLine(Source + " | MD5: " + md5Source);
                        Console.WriteLine(Destination + " | MD5: " + md5Destination);
                        Console.WriteLine("MD5 match! File was copied OK!" + Environment.NewLine);

                    }
                    else
                    {
                        Console.WriteLine(Source + " | MD5: " + md5Source);
                        Console.WriteLine(Destination + " | MD5: " + md5Destination);
                        File.Delete(Destination);
                        Console.WriteLine("MD5 dose not match! File was not copied." + Environment.NewLine);

                    }
                }
                else
                {
                    var files = Directory.GetFiles(dlocation);
                    
                    foreach (var file in files)
                    {
                        Source = file;

                        if (!Source.Contains(":") || !Source.Contains(@"\"))
                        {

                            Source = dlocation + "\\" + Source;

                        }
                        using (var md5 = MD5.Create())
                        {
                            if (File.Exists(Source))
                            {
                                using (var stream = File.OpenRead(Source))
                                {
                                    var hash = md5.ComputeHash(stream);
                                    md5Source = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

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
                            Destination = dlocation + "\\" + Destination;
                        }

                        //copy module
                        if (File.Exists(Source))
                        {
                            if (!File.Exists(Destination))
                            {
                                Console.WriteLine("Copy file...\r\n");
                                File.Copy(Source, Destination);
                            }
                            else
                            {
                                int index = 0;
                                int count= Regex.Matches(Destination, @"\\").Count;
                                string delilmiterSplit = Destination.Split('\\')[count];
                                if(delilmiterSplit.Contains(") - "))
                                {
                                    string fileNameSplit = delilmiterSplit.Split(')')[1];
                                    if(fileNameSplit.StartsWith(" - "))
                                    {
                                        string fileNameSplit2 = delilmiterSplit.Split(')')[0];
                                        fileNameSplit2 = fileNameSplit2.Replace("(", "");
                               

                                        fileNameSplit = fileNameSplit.Split('-')[1].Trim();
                                        int FileCount = Regex.Matches(string.Join(";", files),fileNameSplit).Count;

                                        Console.WriteLine(fileNameSplit);
                                        FileCount--;
                                        Destination = dlocation + "\\(" + FileCount + ") - " + fileNameSplit;
                                        Console.WriteLine("Copy file...\r\n");
                                        File.Copy(Source, Destination);
                                    }

                                }
                                else
                                {
                                    Destination = dlocation + "\\(" + index + ") - " + delilmiterSplit;
                                    Console.WriteLine("Copy file...\r\n");
                                    File.Copy(Source, Destination);
                                }
                                
                                //Console.WriteLine($"Destination file '{Destination}' already exist!" + Environment.NewLine);
                                

                            }
                        }
                        else
                        {
                            Console.WriteLine($"Source file '{Source}' dose not exist!" + Environment.NewLine);
                            Environment.Exit(-1);

                        }
                        //-------------------------
                        //Grabing destination file md5
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(Destination))
                            {
                                var hash = md5.ComputeHash(stream);
                                md5Destination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                            }
                        }



                        Thread.Sleep(1000);
                        if (md5Source == md5Destination)
                        {
                            Console.WriteLine(Source + " | MD5: " + md5Source);
                            Console.WriteLine(Destination + " | MD5: " + md5Destination);
                            Console.WriteLine("MD5 match! File was copied OK!" + Environment.NewLine);

                        }
                        else
                        {
                            Console.WriteLine(Source + " | MD5: " + md5Source);
                            Console.WriteLine(Destination + " | MD5: " + md5Destination);
                            File.Delete(Destination);
                            Console.WriteLine("MD5 dose not match! File was not copied." + Environment.NewLine);

                        }
                    }
                }
            }
            catch (Exception x)
            {
                if (x.ToString().Contains("already exists"))
                {
                    if (!x.ToString().Contains(") - "))
                    {
                        if (File.Exists(Destination))
                        {
                            Console.WriteLine("\r\nFile '" + Destination + "' already exits");
                            Console.WriteLine(x.ToString());
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Command should look like this: fcopy source_file target_file");
                    Console.WriteLine(x.ToString());
                }
            }
        }
        public class ConsoleSpiner
        {
            int counter;
            public ConsoleSpiner()
            {
                counter = 0;
            }
            public void Turn()
            {
                counter++;
                switch (counter % 4)
                {
                    case 0: Console.Write("/"); break;
                    case 1: Console.Write("-"); break;
                    case 2: Console.Write("\\"); break;
                    case 3: Console.Write("|"); break;
                }
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }
        }
    }
  
}
