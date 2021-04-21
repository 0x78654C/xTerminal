using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core;

namespace FMove
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine(" ");
            //reading current location(for test no, after i make dynamic)
            string dlocation = File.ReadAllText(FileSystem.CurrentLocation);
            string cLocation = Directory.GetCurrentDirectory();

            string md5Source = null;
            string md5Destination = null;
            string Source = null;
            string Destination = null;
            try
            {

                Source = args[0];

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
                        Console.WriteLine("Moveing files...\r\n");
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
                    File.Delete(Source);
                    Console.WriteLine("MD5 match! File was moved OK!" + Environment.NewLine);

                }
                else
                {
                    Console.WriteLine(Source + " | MD5: " + md5Source);
                    Console.WriteLine(Destination + " | MD5: " + md5Destination);
                    File.Delete(Destination);
                    Console.WriteLine("MD5 dose not match! File was not moved." + Environment.NewLine);

                }

            }
            catch (Exception x)
            {
                if (x.ToString().Contains("already exists"))
                {
                    if (File.Exists(Destination))
                    {
                        Console.WriteLine("\r\nFile '" + Destination + "' already exits");
                    }
                    if (Directory.Exists(Destination))
                    {
                        Console.WriteLine("\r\nDirectory '" + Destination + "' already exits");
                    }
                }
                else
                {
                    Console.WriteLine("Command should look like this: fmove source_file target_file");
                    Console.WriteLine(x.ToString());
                }
            }
        }

    }
}
