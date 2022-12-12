using System.Text.Json.Serialization;

namespace OpenAI.Api.Client.Models
{
    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("model")]
        public int TotalTokens { get; set; }
    }
}