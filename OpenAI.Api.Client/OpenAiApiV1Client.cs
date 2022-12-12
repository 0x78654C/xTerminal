using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OpenAI.Api.Client.Models;

namespace OpenAI.Api.Client
{
    // Credits to @mkbmain for the very nice work : https://github.com/mkbmain/OpenAI.Api.Client
    public class OpenAiApiV1Client
    {
        private readonly HttpClient _httpClient;

        public OpenAiApiV1Client(HttpClient httpClient, string apiKey, string organisation = null)
        {
            _httpClient = httpClient;
            if (_httpClient.BaseAddress?.AbsoluteUri.Contains("https://api.openai.com/v1/") ?? false) return;
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            if (organisation is null) return;
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", organisation);
        }

        public virtual Task<OpenAiModelsResponse> GetModels() => Request<OpenAiModelsResponse>(new HttpRequestMessage(HttpMethod.Get, "models"));

        public virtual Task<OpenAiModel> GetModel(string model) => Request<OpenAiModel>(new HttpRequestMessage(HttpMethod.Get, $"models/{model}"));

        public virtual Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest) => Request<CompletionResponse>(new HttpRequestMessage(HttpMethod.Post, "completions")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(completionRequest), Encoding.Default, "application/json"),
        });

        private async Task<T> Request<T>(HttpRequestMessage message) =>
            await System.Text.Json.JsonSerializer.DeserializeAsync<T>(await (await _httpClient.SendAsync(message)).Content.ReadAsStreamAsync());
    }
}