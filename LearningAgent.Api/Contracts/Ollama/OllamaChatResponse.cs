using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama
{
    public class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage Message { get; set; } = new();
    }
}
