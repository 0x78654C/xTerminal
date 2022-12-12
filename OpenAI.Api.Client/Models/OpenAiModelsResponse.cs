using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAI.Api.Client.Models
{
    public class OpenAiModelsResponse
    {
        [JsonPropertyName("object")]
        public string @Object { get; set; }

        [JsonPropertyName("data")]
        public List<OpenAiModel> Data { get; set; }
    }
}