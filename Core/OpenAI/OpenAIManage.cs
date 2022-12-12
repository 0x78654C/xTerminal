using AI = OpenAI.Api.Client;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace Core.OpenAI
{
    public class OpenAIManage
    {
        private string _apiKey = string.Empty;
        private string _question = string.Empty;
        private static HttpClient _httpClient= new HttpClient();

        public OpenAIManage(string apiKey, string question)
        {
            _apiKey = apiKey;
            _question = question;
        }

        public async Task<string> AskOpenAI()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "";

            if(string.IsNullOrWhiteSpace(_question))
                return "";

            AI.OpenAiApiV1Client v1Client = new AI.OpenAiApiV1Client(_httpClient, _apiKey);

            var result = await v1Client.PostCompletion(new AI.Models.CompletionRequest
            {
                MaxTokens = 999,
                Temperature = 0.8m,
                Model = "text-davinci-003",
                Prompt=_question
            });
            return result.Choices.First().Text; 
        }
    }
}
