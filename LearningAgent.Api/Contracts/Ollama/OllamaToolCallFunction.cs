using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement Arguments { get; set; }
}
