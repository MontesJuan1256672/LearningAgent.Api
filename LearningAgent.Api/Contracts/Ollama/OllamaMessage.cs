namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}