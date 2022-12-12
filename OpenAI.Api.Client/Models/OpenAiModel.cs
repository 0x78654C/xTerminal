using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAI.Api.Client.Models
{
    public class OpenAiModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Type { get; set; }

        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; }

        [JsonPropertyName("permission")]
        public List<Permission> Permission { get; set; }

        [JsonPropertyName("root")]
        public string Root { get; set; }

        [JsonPropertyName("parent")]
        public object Parent { get; set; }
    }
}