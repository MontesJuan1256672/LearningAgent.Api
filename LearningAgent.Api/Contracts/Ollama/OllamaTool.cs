using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OllamaToolFunction Function { get; set; } = new();
}       



