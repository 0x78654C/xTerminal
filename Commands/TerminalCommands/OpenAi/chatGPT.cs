using System;
using System.Threading.Tasks;
using Core;
using System.Runtime.Versioning;
using Core.OpenAI;
using System.IO;
using Core.Encryption;
using OpenRouter;
using static System.Collections.Specialized.BitVector32;

namespace Commands.TerminalCommands.OpenAi
{
    [SupportedOSPlatform("windows")]

    /* Command for run chatGPT from OpenAI in terminal */
    public class chatGPT : ITerminalCommand
    {
        public string Name => "cgpt";
        private static string s_helpMessage = @"Usage of cgpt command:
    Info: This command allows you to interact with OpenAI's chatGPT model or OpenRouter's models directly from the terminal.

    Example 1: cgpt -setkey key_from_openai (Store the API key provided by OpenAI or OpenRouter)
    Example 2: cgpt question_you_want_to_ask (Display the answer for your question)
";

        public void Execute(string arg)
        {
            try
            {
                var apiKey = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regOpenAI_APIKey);
                var decryptedKey = "";
                if (!string.IsNullOrEmpty(apiKey))
                    decryptedKey = DPAPI.Decrypt(apiKey);
                GlobalVariables.isErrorCommand = false;
                if (arg == Name)
                {
                    FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage!");
                    return;
                }
                if (arg == $"{Name} -h")
                {
                    Console.WriteLine(s_helpMessage);
                    return;
                }

                if (arg.Contains("-setkey"))
                {
                    FileSystem.SuccessWriteLine("Enter AI API key: ");
                    var getConsoleKey = FileSystem.GetHiddenConsoleInput();

                    var encryptKey = DPAPI.Encrypt(getConsoleKey.ConvertSecureStringToString());
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regOpenAI_APIKey, encryptKey);
                    Console.WriteLine();
                    if(getConsoleKey.ConvertSecureStringToString().StartsWith("sk-or"))
                        FileSystem.SuccessWriteLine("OpenRouter API key is stored!");
                    else
                        FileSystem.SuccessWriteLine("OpenAI API key is stored!");
                    return;
                }
                if (string.IsNullOrEmpty(decryptedKey))
                {
                    FileSystem.ErrorWriteLine("No OpenAI/OpenRouter API key was found. Use -setKey to store your API key!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                var question = arg.SplitByText(Name, 1);

                if (string.IsNullOrWhiteSpace(question))
                {
                    FileSystem.ErrorWriteLine("You need to provide a question!");
                    GlobalVariables.isErrorCommand = true;
                    return;
                }
                GetOpenAIData(question, decryptedKey).Wait();
            }
            catch (Exception e)
            {
                FileSystem.ErrorWriteLine(e.Message);
                GlobalVariables.isErrorCommand = true;
            }
        }

        /// <summary>
        /// Display output from OpenAI.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="apiKey"></param>
        public async Task GetOpenAIData(string question, string apiKey)
        {
            if (!GlobalVariables.isPipeCommand)
            {
                if(apiKey.StartsWith("sk-or"))
                    FileSystem.SuccessWriteLine("Loading data from OpenRouter:");
                else
                    FileSystem.SuccessWriteLine("Loading data from OpenAI:");
            }
            StringReader reader;
            if (apiKey.StartsWith("sk-or"))
            {
                var openRouterClient = new OpenRouterClient(apiKey);
                reader = new StringReader(await openRouterClient.SendPromptAsync(question));
            }
            else
            {
                var openAI = new OpenAIManage(apiKey, question.Trim());
                reader = new StringReader(await openAI.AskOpenAI());
            }
            string line = "";
            while ((line = reader.ReadLine()) != null)
            {
                if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                    GlobalVariables.pipeCmdOutput += $"{Environment.NewLine}{line}";
                else
                    Console.Write($"{Environment.NewLine}{line}");
                await Task.Delay(200);
            }
            Console.WriteLine();
        }
    }
}
