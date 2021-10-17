using Core;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Commands.TerminalCommands.DirFiles
{
    public class MD5Check : ITerminalCommand
    {

        /*
         * Checks MD5 hash of a file.
         */
        public string Name => "md5";

        public void Execute(string arg)
        {

            try
            {
                string cDir = File.ReadAllText(GlobalVariables.currentDirectory);
                int argLenght = arg.Length - 4;
                string input = arg.Substring(4, argLenght);

                if (input.Contains(":") && input.Contains(@"\"))
                {
                    if (File.Exists(input))
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(input))
                            {
                                var hash = md5.ComputeHash(stream);
                                Console.WriteLine("MD5 of " + input + ": " + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
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
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message} Have you typed the file name?");
            }
        }
    }
}
