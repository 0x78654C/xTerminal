﻿using Core;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;

namespace Commands.TerminalCommands.ConsoleSystem
{
    /*
     Checks the permission attributes for a file or directory. 
     */
    [SupportedOSPlatform("windows")]
    class CheckPermission : ITerminalCommand
    {
        public string Name => "cp";
        public void Execute(string arg)
        {
            string currentLocation = File.ReadAllText(GlobalVariables.currentDirectory); ;
            string input;
            try
            {
                GlobalVariables.isErrorCommand = false;
                string tabs = "\t";
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    input = GlobalVariables.pipeCmdOutput.Trim() ;
                else
                {
                    int argLenght = arg.Length - 3;
                    input = arg.Substring(3, argLenght);
                }
                ListPermissions(input, currentLocation, tabs);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine($"{e.Message}. You must type the file/directory name!");
                GlobalVariables.isErrorCommand = true;
            }
        }

        // List permissions of a file or directory.
        private void ListPermissions(string input, string currentDirectory, string tabs)
        {
            if (GlobalVariables.isPipeCommand)
                GlobalVariables.pipeCmdOutput = "";
            input = FileSystem.SanitizePath(input, currentDirectory);
            if (Directory.Exists(input))
            {
                DirectoryInfo dInfo = new DirectoryInfo(input);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                Console.WriteLine("Permissions of directory: " + input);
                foreach (FileSystemAccessRule ace in acl)
                    PermissionOut(ace, tabs);
            }
            else
            {
                FileInfo dInfo = new FileInfo(input);
                FileSecurity dSecurity = dInfo.GetAccessControl();
                AuthorizationRuleCollection acl = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                Console.WriteLine("Permissions of file: " + input);
                foreach (FileSystemAccessRule ace in acl)
                    PermissionOut(ace, tabs);
            }
        }

        // Ouput the permission of a file or directory.
        private void PermissionOut(FileSystemAccessRule ace, string tabs)
        {
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 && GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
            {
                GlobalVariables.pipeCmdOutput += string.Format("{0}Account: {1}\n {0}Type: {2}\n {0}Rights: {3}\n {0}Inherited: {4}\n",
                     tabs,
                     ace.IdentityReference.Value,
                     ace.AccessControlType,
                     ace.FileSystemRights,
                     ace.IsInherited);
            }
            else
            {
                Console.WriteLine("{0}Account: {1}\n {0}Type: {2}\n {0}Rights: {3}\n {0}Inherited: {4}\n",
          tabs,
          ace.IdentityReference.Value,
          ace.AccessControlType,
          ace.FileSystemRights,
          ace.IsInherited);
            }
        }
    }
}
