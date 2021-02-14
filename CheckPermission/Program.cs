using System;
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

            string input;
            try
            {
                string tabs = "\t";
                input = args[0];
                if (Directory.Exists(input))
                {
                    DirectoryInfo dInfo = new DirectoryInfo(input);
                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
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
            catch(Exception e)
            {
                Console.WriteLine("Check Permission error: "+e.ToString());              
            }
        }
    }
}
