using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("function")]
    public OllamaToolCallFunction Function { get; set; } = new();
}


