﻿using System;
using System.Threading.Tasks;
using Core;
using System.Runtime.Versioning;
using Core.OpenAI;
using System.IO;

namespace Commands.TerminalCommands.OpenAi
{
    [SupportedOSPlatform("windows")]

    /* Command for run chatGPT from OpenAI in terminal */
    public class chatGPT : ITerminalCommand
    {
        public string Name => "cgpt";
        private static string s_helpMessage = @"Usage of cgpt command:

    Example 1: cgpt -setkey key_from_openai (Store the API key provided by OpenAI.com)
    Example 2: cgpt question_you_want_to_ask (Display the answer for your question)
";
        OpenAIManage openAI;
        public void Execute(string arg)
        {

            try
            {
                var apiKey = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regOpenAI_APIKey);

                if (arg.Length == 4)
                {
                    Console.WriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (arg.Contains("-setkey"))
                {
                    var getConsoleKey = arg.SplitByText("-setkey ", 1).Trim();
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regOpenAI_APIKey, getConsoleKey);
                    Console.WriteLine("OpenAI API key is stored!");
                    return;
                }
                if (string.IsNullOrEmpty(apiKey))
                {
                    FileSystem.ErrorWriteLine("No OpenAI API key was found. Use -setKey to store your API key!");
                    return;
                }
                var question = arg.SplitByText(Name, 1);

                if (string.IsNullOrWhiteSpace(question))
                {
                    FileSystem.ColorConsoleTextLine(ConsoleColor.Yellow, "You need to provide a question!");
                    return;
                }

                GetOpenAIData(question, apiKey).Wait();
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
            }
        }

        /// <summary>
        /// Display output from OpenAI.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="apiKey"></param>
        public async Task GetOpenAIData(string question, string apiKey)
        {
            Console.WriteLine("Loading data from OpenAI:");
            openAI = new OpenAIManage(apiKey, question.Trim());
            StringReader reader = new StringReader(await openAI.AskOpenAI());
            string line = "";
            while ((line = reader.ReadLine()) != null)
            {
                Console.Write($"{Environment.NewLine}{line}");
                await Task.Delay(200);
            }
            Console.WriteLine();
        }
    }
}
