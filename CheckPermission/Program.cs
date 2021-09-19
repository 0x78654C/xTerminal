using Core;
using System;
using System.IO;
using System.Security.AccessControl;

namespace CheckPermission
{
    class Program
    {
        /*
         Check file/folder access permission
         */
        static void Main(string[] args)
        {
            string currentLocation = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regCurrentDirectory);;
            string input = "";
            try
            {
                string tabs = "\t";
                input = args[0];
                if (input.Contains(@"\"))
                {
                    if (Directory.Exists(input))
                    {
                        DirectoryInfo dInfo = new DirectoryInfo(input);
                        DirectorySecurity dSecurity = dInfo.GetAccessControl();
                        AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                        Console.WriteLine("Permissions of directory: " + input);
                        foreach (FileSystemAccessRule ace in acl)
                        {

                            Console.WriteLine("{0}Account: {1}", tabs, ace.IdentityReference.Value);
                            Console.WriteLine("{0}Type: {1}", tabs, ace.AccessControlType);
                            Console.WriteLine("{0}Rights: {1}", tabs, ace.FileSystemRights);
                            Console.WriteLine("{0}Inherited: {1}", tabs, ace.IsInherited);
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        FileInfo dInfo = new FileInfo(input);
                        FileSecurity dSecurity = dInfo.GetAccessControl();
                        AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                        Console.WriteLine("Permissions of file: " + input);
                        foreach (FileSystemAccessRule ace in acl)
                        {
                            Console.WriteLine("{0}Account: {1}", tabs, ace.IdentityReference.Value);
                            Console.WriteLine("{0}Type: {1}", tabs, ace.AccessControlType);
                            Console.WriteLine("{0}Rights: {1}", tabs, ace.FileSystemRights);
                            Console.WriteLine("{0}Inherited: {1}", tabs, ace.IsInherited);
                            Console.WriteLine();
                        }
                    }
                }
                else
                {
                    if (Directory.Exists(currentLocation + @"\" + input))
                    {
                        DirectoryInfo dInfo = new DirectoryInfo(currentLocation + @"\" + input);
                        DirectorySecurity dSecurity = dInfo.GetAccessControl();
                        AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                        Console.WriteLine("Permissions of directory: " + currentLocation + @"\" + input);
                        foreach (FileSystemAccessRule ace in acl)
                        {

                            Console.WriteLine("{0}Account: {1}", tabs, ace.IdentityReference.Value);
                            Console.WriteLine("{0}Type: {1}", tabs, ace.AccessControlType);
                            Console.WriteLine("{0}Rights: {1}", tabs, ace.FileSystemRights);
                            Console.WriteLine("{0}Inherited: {1}", tabs, ace.IsInherited);
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        FileInfo dInfo = new FileInfo(currentLocation + @"\" + input);
                        FileSecurity dSecurity = dInfo.GetAccessControl();
                        AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                        Console.WriteLine("Permissions of file: " + currentLocation + @"\" + input);
                        foreach (FileSystemAccessRule ace in acl)
                        {
                            Console.WriteLine("{0}Account: {1}", tabs, ace.IdentityReference.Value);
                            Console.WriteLine("{0}Type: {1}", tabs, ace.AccessControlType);
                            Console.WriteLine("{0}Rights: {1}", tabs, ace.FileSystemRights);
                            Console.WriteLine("{0}Inherited: {1}", tabs, ace.IsInherited);
                            Console.WriteLine();
                        }
                    }
                }
            }
            catch(Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message} You must type the file/directory name!");
            }
        }
    }
}
