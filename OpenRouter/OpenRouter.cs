// Open router api call

using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace OpenRouter
{
    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class RequestBody
    {
        public string model { get; set; }
        public List<Message> messages { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class OpenRouterResponse
    {
        public List<Choice> choices { get; set; }
    }

    public class OpenRouterClient
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";
        private readonly string _apiKey;

        public OpenRouterClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("X-Title", "xTerminal");
        }

        public async Task<string> SendPromptAsync(string prompt, string model = "openai/gpt-3.5-turbo")
        {
            var requestBody = new RequestBody
            {
                model = model,
                messages = new List<Message>
                {
                    new Message { role = "user", content = prompt }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API error: {response.StatusCode}\n{responseContent}");
            }

            var result = JsonConvert.DeserializeObject<OpenRouterResponse>(responseContent);
            return result?.choices?[0]?.message?.content ?? "No response";
        }
    }
}
