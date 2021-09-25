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
        public static void ProcessExecute(string input, string arguments, bool fileCheck)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(input)
            {
                Arguments = arguments
            };

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
