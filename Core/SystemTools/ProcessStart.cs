using System.IO;
using System.Diagnostics;


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
        public static void ProcessExecute(string input, string arguments, bool fileCheck, bool asAdmin)
        {
            var process = new Process();
            if (asAdmin)
            {
                process.StartInfo = new ProcessStartInfo(input)
                {
                    Arguments = arguments,
                    Verb = "runas"
                };
            }
            else
            {
                process.StartInfo = new ProcessStartInfo(input)
                {
                    Arguments = arguments
                };
            }

            if (fileCheck)
            {
                if (File.Exists(input))
                {
                    process.Start();
                    return;
                }
                FileSystem.ErrorWriteLine($"Couldn't find file \"{input}\" to execute");
                return;
            }
            process.Start();
        }
    }
}
