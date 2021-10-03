using System;
using System.IO;
using System.Security.Cryptography;
using Core;

namespace Commands.TerminalCommands.DirFiles
{
    public class MD5Check : ITerminalCommand
    {
        public string Name => "md5";

        public void Execute(string arg)
        {

            try
            {
                string cDir = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory); ;
                string input = arg.Split(' ')[1];

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
