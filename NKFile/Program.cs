using Core;
using System;
using System.IO;

namespace NKFile
{
    class Program
    {
        static void Main(string[] args)
        {
            string CLocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);;
            string file;
            try
            {
                file = args[0];
                if (Directory.Exists(CLocation))
                {
                    File.Create(CLocation + @"\" + file);
                }
                else
                {
                    FileSystem.ErrorWriteLine("Directory dose not exist!");
                }
            }
            catch
            {
                FileSystem.ErrorWriteLine("You must type the file name!");
            }
        }
    }
}
