﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace CheckPermission
{
    class Program
    {
        /*
         Check file/folder access permission
         */
        static void Main(string[] args)
        {
            string CurrentLocation = File.ReadAllText(FileSystem.CurrentLocation);
            string input="";
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
                    if (Directory.Exists(CurrentLocation+@"\"+ input))
                    {
                        DirectoryInfo dInfo = new DirectoryInfo(CurrentLocation + @"\" + input);
                        DirectorySecurity dSecurity = dInfo.GetAccessControl();
                        AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                        Console.WriteLine("Permissions of directory: " + CurrentLocation + @"\" + input);
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
                        FileInfo dInfo = new FileInfo(CurrentLocation + @"\" + input);
                        FileSecurity dSecurity = dInfo.GetAccessControl();
                        AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                        Console.WriteLine("Permissions of file: " + CurrentLocation + @"\" + input);
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
            catch
            {
                Console.WriteLine("You must type the file/directory name!");
                
            }
        }
    }
}
