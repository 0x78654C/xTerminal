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

            string crcSource = null;
            string crcDestination = null;
            string Source = null;
            string Destination = null;
            try
            {

                Source = args[0];

                if (!Source.Contains(":") || !Source.Contains(@"\"))
                {

                    Source = dlocation + "\\" + Source;

                }
                using (var crc = Crc32.Create())
                {
                    if (File.Exists(Source))
                    {
                        using (var stream = File.OpenRead(Source))
                        {
                            var hash = crc.ComputeHash(stream);
                            crcSource = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

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
                //Grabing destination file crc
                using (var crc = Crc32.Create())
                {
                    using (var stream = File.OpenRead(Destination))
                    {
                        var hash = crc.ComputeHash(stream);
                        crcDestination = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                    }
                }



                Console.WriteLine("Verifying file...\r\n");
                if (crcSource == crcDestination)
                {
                    Console.WriteLine(Source + " | CRC: " + crcSource);
                    Console.WriteLine(Destination + " | CRC: " + crcDestination);
                    File.Delete(Source);
                    Console.WriteLine("CRC match! File was moved OK!" + Environment.NewLine);

                }
                else
                {
                    Console.WriteLine(Source + " | CRC: " + crcSource);
                    Console.WriteLine(Destination + " | CRC: " + crcDestination);
                    File.Delete(Destination);
                    Console.WriteLine("CRC dose not match! File was not moved." + Environment.NewLine);

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
