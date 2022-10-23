using System;
using System.Diagnostics;
using System.IO;

namespace Core.SystemTools
{
    public class ProcessStart
    {
        /// <summary>
        /// Process execution with arguments.
        /// </summary>
        /// <param name="input">File name.</param>
        /// <param name="arguments">Specific file arguments.</param>
        /// <param name="fileCheck">Check file if exists before process exection. </param>
        /// <param name="asAdmin">Run as different user.</param>
        public static void ProcessExecute(string input, string arguments, bool fileCheck, bool asAdmin, bool waitForExit)
        {
            try
            {
                var process = new Process();

                if (asAdmin)
                {
                    process.StartInfo = new ProcessStartInfo(input);
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    if (!waitForExit)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardError = true;
                    }
                    process.StartInfo.Verb = "runas";
                }
                else
                {
                    process.StartInfo = new ProcessStartInfo(input);
                    process.StartInfo.UseShellExecute = false;
                    if (!waitForExit)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardError = true;
                    }
                    process.StartInfo.Arguments = arguments;
                }

                if (fileCheck)
                {
                    if (File.Exists(input))
                    {
                        process.Start();
                        if (waitForExit)
                            process.WaitForExit();
                        return;
                    }
                    FileSystem.ErrorWriteLine($"Couldn't find file \"{input}\" to execute");
                    return;
                }
                process.Start();
                if (waitForExit)
                    process.WaitForExit();
            }
            catch (System.ComponentModel.Win32Exception win)
            {
                FileSystem.ErrorWriteLine(win.Message);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Process execution in separate window with no args.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="asAdmin"></param>
        public static void ProcessExecute(string input, bool asAdmin)
        {
            try
            {
                var process = new Process();

                if (asAdmin)
                {
                    process.StartInfo = new ProcessStartInfo(input)
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                }
                else
                {
                    process.StartInfo = new ProcessStartInfo(input)
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    };
                }
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception win)
            {
                FileSystem.ErrorWriteLine(win.Message);
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }
    }
}
