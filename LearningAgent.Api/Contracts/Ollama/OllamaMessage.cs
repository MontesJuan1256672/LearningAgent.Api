using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
   
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
   
    [JsonPropertyName("tool_calls")] public List<OllamaToolCall> ToolCalls { get; set; } = [];
   
    [JsonPropertyName("tool_name")] public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("tool_call_id")] public string ToolCallId { get; set; } = string.Empty;  
}

