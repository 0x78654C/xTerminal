using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace CheckMD5
{
    /* MD5 file checker*/
    class Program
    {

        static void Main(string[] args)
        {
            try { 

            string cDir = File.ReadAllText(FileSystem.CurrentLocation);
            
            string input = args[0];
       
                if (input.Contains(":") && input.Contains(@"\"))
                {
                    if (File.Exists(input))
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(input))
                            {
                                var hash = md5.ComputeHash(stream);
                                Console.WriteLine("MD5 of "+input+": "+BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());

                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("File " + input + " dose not exist!");
                    }
                }
                else
                {
                    if (File.Exists(cDir + @"\" + input))
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(cDir + @"\" + input))
                            {
                                var hash = md5.ComputeHash(stream);
                                Console.WriteLine("MD5 of " + input + ": " + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());

                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("File " + cDir + @"\" + input + " dose not exist!");
                    }
                }
            }
            catch 
            {
                Console.WriteLine("Error: Have you typed the file name?");
            }
        }

    }
}



