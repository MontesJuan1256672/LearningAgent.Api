using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaToolCall
{
    [JsonPropertyName("function")]
    public OllamaToolCallFunction Function { get; set; } = new();
}


