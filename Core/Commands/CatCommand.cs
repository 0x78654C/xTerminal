using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Core.Commands
{
    public class CatCommand
    {

        /// <summary>
        /// Output to console a file as string.
        /// </summary>
        /// <param name="input">File name.</param>
        /// <param name="currentDir">Current location.</param>
        public static string FileOutput(string input, string currentDir)
        {
            string output = string.Empty;
            if (input.Contains(":") && input.Contains(@"\"))
            {
                if (File.Exists(input))
                {
                    using (StreamReader streamReader = new StreamReader(input))
                    {
                        output += streamReader.ReadToEnd();
                        streamReader.Close();
                    }
                }
                else
                {
                    Console.WriteLine("File " + input + " dose not exist!");
                }
            }
            else
            {
                if (File.Exists(currentDir + @"\" + input))
                {
                    Console.WriteLine(currentDir + @"\" + input);
                    using (StreamReader streamReader = new StreamReader(currentDir + @"\" + input))
                    {
                        output += streamReader.ReadToEnd();
                        streamReader.Close();
                    }
                }
                else
                {
                    FileSystem.ErrorWriteLine("File " + currentDir + @"\" + input + " dose not exist!");
                }
            }
            return output;
        }

        /// <summary>
        /// Output to console specific lines from a file containging a specific string.
        /// </summary>
        /// <param name="searchString"> Search parameter. </param>
        /// <param name="currentDir">Current directory. </param>
        /// <param name="input"> File name to search in. </param>
        /// <param name="savedFile"> File name where to store the result data. </param>
        /// <returns>string</returns>
        public static string FileOutput(string searchString, string currentDir, string input, string savedFile)
        {
            string output = string.Empty;
           
            if (input.Contains(":") && input.Contains(@"\"))
            {
                if (File.Exists(input))
                {
                    using (StreamReader streamReader = new StreamReader(input))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (line.ToLower().Contains(searchString.ToLower()))
                            {
                                output += line + Environment.NewLine;
                            }
                        }
                        streamReader.Close();
                    }
                }
                else
                {
                    Console.WriteLine("File " + input + " dose not exist!");
                }
            }
            else
            {
                if (File.Exists(currentDir + @"\" + input))
                {
                    using (StreamReader streamReader = new StreamReader(currentDir + @"\" + input))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (line.ToLower().Contains(searchString.ToLower()))
                            {
                                output += line + Environment.NewLine;
                            }
                        }
                        streamReader.Close();
                    }
                }
                else
                {
                    FileSystem.ErrorWriteLine("File " + currentDir + @"\" + input + " dose not exist!");
                }
            }
            if (savedFile.Length > 0)
            {
                if (savedFile.Contains(":") && savedFile.Contains(@"\"))
                {
                    File.WriteAllText(savedFile, output);
                    output = $"Data saved in {savedFile}";
                }
                else
                {
                    File.WriteAllText(currentDir + @"\" + savedFile, output);
                    output = $"Data saved in {savedFile}";
                }
            }
            return output;
        }

        /// <summary>
        /// Output to console lines that contains a specific string.
        /// </summary>
        /// <param name="searchString"> Search parameter. </param>
        /// <param name="currentDir">Current directory. </param>
        /// <param name="input"> File name to search in. </param>
        /// <param name="savedFile"> File name where to store the result data. </param>
        /// <returns>string</returns>
        public static string MultiFileOutput(string searchString, string currentDir, string input, string savedFile)
        {
            string[] files = input.Split(' ').ToArray();
            
            string output = string.Empty;
            foreach (var file in files)
            {
                if (File.Exists(currentDir + @"\" + file))
                {
                    output += $"---------------- {file} ----------------\n";
                    using (StreamReader streamReader = new StreamReader(currentDir + @"\" + file))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (line.ToLower().Contains(searchString.ToLower()))
                            {
                                output += line + Environment.NewLine;
                            }
                        }
                        streamReader.Close();
                    }
                    output +=Environment.NewLine;
                }
                else
                {
                    FileSystem.ErrorWriteLine("File " + currentDir + @"\" + file + " dose not exist!");
                }
            }
            if (savedFile.Length > 0)
            {
                if (savedFile.Contains(":") && savedFile.Contains(@"\"))
                {
                    File.WriteAllText(savedFile, output);
                    output = $"Data saved in {savedFile}";
                }
                else
                {
                    File.WriteAllText(currentDir + @"\" + savedFile, output);
                    output = $"Data saved in {savedFile}";
                }
            }
            return output;
        }
    }
}
