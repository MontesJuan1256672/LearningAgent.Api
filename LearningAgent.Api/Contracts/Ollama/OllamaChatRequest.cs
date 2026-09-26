using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaChatRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
   
    [JsonPropertyName("messages")] public List<OllamaMessage> Messages { get; set; } = [];
   
    [JsonPropertyName("tools")] public List<OllamaTool> Tools { get; set; } = [];
   
    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
   
}