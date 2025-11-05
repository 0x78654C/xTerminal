using Microsoft.Extensions.AI;
using System.Diagnostics;

namespace OllamaInt
{
    public class OllamaLLM
    {
        public string Model { get; set; }
        public string Uri { get; set; }
        public string Promt { get; set; } = string.Empty;

        public List<ChatMessage> ChatHistory { get; set; }

        /// <summary>
        /// Constructor for Ollama.
        /// </summary>
        public OllamaLLM()
        {
        }

        /// <summary>
        /// ASk ollma
        /// </summary>
        /// <returns></returns>
        public async Task<string> AskOllama()
        {
            IChatClient chatClient = new OllamaChatClient(new Uri(Uri), Model);
            // Get user prompt and add to chat history  
            ChatHistory.Add(new ChatMessage(ChatRole.User, Promt));

            // Stream the AI response and add to chat history  
            var response = "";
            await foreach (var item in chatClient.GetStreamingResponseAsync(ChatHistory))
            {
                response += item.Text;
            }

            ChatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
            return response;
        }


        /// <summary>
        /// Check if ollama is installed.
        /// </summary>
        /// <returns></returns>
        public bool IsOllamaInstalled()
        {
            var startInfo = new ProcessStartInfo("cmd")
            {
                Arguments = "/c ollama -h",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            var outData = process.StandardOutput.ReadToEnd();
            return outData.Contains("ollama [flags]");
        }

        /// <summary>
        /// List models from ollama
        /// </summary>
        /// <returns></returns>
        public List<string> LocalModels()
        {
            var modelList = new List<string>();
            var startInfo = new ProcessStartInfo("cmd")
            {
                UseShellExecute = false,
                Arguments = "/c ollama list",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            var outData = process.StandardOutput.ReadToEnd();
            using var reader = new StringReader(outData);
            string line;
            while (null != (line = reader.ReadLine()))
            {
                if (line.StartsWith("NAME")) continue;
                var model = line.Split(' ')[0].Trim();
                if (string.IsNullOrEmpty(model)) continue;
                modelList.Add(model);
            }

            return modelList;
        }
    }
}