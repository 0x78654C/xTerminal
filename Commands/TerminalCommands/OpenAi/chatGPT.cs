using Core;
using Core.Encryption;
using Core.OpenAI;
using OllamaInt;
using OpenRouter;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.OpenAi
{
    [SupportedOSPlatform("windows")]

    /* Command for run chatGPT from OpenAI, OpenRouter and Ollama in terminal */
    public class chatGPT : ITerminalCommand
    {
        public string Name => "cgpt";
        private static string s_helpMessage = @"Usage of cgpt command:
    Info: This command allows you to interact with OpenAI's chatGPT, OpenRouter or Ollama models directly from the terminal.

    cgpt -setkey key_from_openai       : Store the API key provided by OpenAI or OpenRouter
    cgpt <question_you_want_to_ask>    : Display the answer for your question.
    
    Ollama parameters:
    cgpt -l                            : Will list the Ollama models.
    cgpt -m <model_name>               : Set mode to use with Ollama.
    cgpt -sm <model_name>              : Set a specific model to use for Ollama.
    cgpt -cm                           : Display current used Ollama model.
    cgpt -o <question_you_want_to_ask> : Display the answer for your question with Ollama.
";

        public void Execute(string arg)
        {
            try
            {
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

                // ---- Ollama parameters ----
                if (arg.Contains("-l"))
                {
                    var ollama = new OllamaLLM();
                    var list = string.Join("\n", ollama.LocalModels());
                    FileSystem.SuccessWriteLine($"Installed Ollama models:");
                    if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                        GlobalVariables.pipeCmdOutput = list;
                    else
                        Console.WriteLine(list);
                    return;
                }

                if (arg.Contains("-sm"))
                {
                    var model = arg.SplitByText("-sm", 1).Trim();
                    if (string.IsNullOrEmpty(model))
                    {
                        FileSystem.ErrorWriteLine("You must enter the Ollama model name!");
                        return;
                    }
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regOllama_Model, model);
                    FileSystem.SuccessWriteLine($"Ollama model '{model}' is set!");
                    return;
                }

                if (arg.Contains("-cm"))
                {

                    var currentModel = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regOllama_Model);
                    if (string.IsNullOrEmpty(currentModel))
                    {
                        FileSystem.ErrorWriteLine($"There is no Ollama model set!");
                        return;
                    }
                    FileSystem.SuccessWriteLine($"Current Ollama model in use: '{currentModel}'");
                    return;
                }


                if (arg.Contains("-o"))
                {
                    var oQuestion = arg.SplitByText("-o", 1).Trim();
                    if (string.IsNullOrEmpty(oQuestion))
                    {
                        FileSystem.ErrorWriteLine("You need to provide a question!");
                        return;
                    }
                    var model = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regOllama_Model);
                    if (string.IsNullOrEmpty(model))
                    {
                        FileSystem.ErrorWriteLine($"There is no Ollama model set!");
                        return;
                    }
                    GetOllamaAIData(oQuestion, model).Wait();
                    return;
                }
                // --------------------

                if (arg.Contains("-setkey"))
                {
                    FileSystem.SuccessWriteLine("Enter AI API key: ");
                    var getConsoleKey = FileSystem.GetHiddenConsoleInput();

                    var encryptKey = DPAPI.Encrypt(getConsoleKey.ConvertSecureStringToString());
                    RegistryManagement.regKey_WriteSubkey(GlobalVariables.regKeyName, GlobalVariables.regOpenAI_APIKey, encryptKey);
                    Console.WriteLine();
                    if (getConsoleKey.ConvertSecureStringToString().StartsWith("sk-or"))
                        FileSystem.SuccessWriteLine("OpenRouter API key is stored!");
                    else
                        FileSystem.SuccessWriteLine("OpenAI API key is stored!");
                    return;
                }
                var apiKey = RegistryManagement.regKey_Read(GlobalVariables.regKeyName, GlobalVariables.regOpenAI_APIKey);
                var decryptedKey = "";
                try
                {
                    if (!string.IsNullOrEmpty(apiKey))
                        decryptedKey = DPAPI.Decrypt(apiKey);
                }
                catch
                {
                    FileSystem.ErrorWriteLine("OpenAI/OpenRouter API key is corrputed, you need to set a new one!");
                    GlobalVariables.isErrorCommand = true;
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
                if (apiKey.StartsWith("sk-or"))
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

        public async Task GetOllamaAIData(string question, string model)
        {
            OllamaLLM ollamaClient = new OllamaLLM();
            ollamaClient.Model = model;
            ollamaClient.Promt = question;
            ollamaClient.Uri = GlobalVariables.ollamaUri;
            ollamaClient.ChatHistory = GlobalVariables.chatHistory;
            var response = await Task.Run(ollamaClient.AskOllama);
            if (GlobalVariables.isPipeCommand && GlobalVariables.pipeCmdCount > 0 || GlobalVariables.pipeCmdCount < GlobalVariables.pipeCmdCountTemp)
                GlobalVariables.pipeCmdOutput = response;
            else
                Console.WriteLine(response);
        }
    }
}
