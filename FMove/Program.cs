using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FMove
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine(" ");
            try
            {
                string md5Source = null;
                string md5Destination = null;
                string Source = null;
                string Destination = null;
                Source =args[0];

                //Grabing source file md5
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(Source))
                    {
                        var hash = md5.ComputeHash(stream);
                        md5Source = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                    }
                }

;
                Destination = args[1] + "_moved";
                Console.WriteLine("Moving files...");
                if (File.Exists(Source))
                {
                    File.Copy(Source, Destination);
                }
                else
                {
                    Console.WriteLine($" File '{Source}' dose not exist!" + Environment.NewLine);
                }

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
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString() + Environment.NewLine);
            }
        }

    }
}
