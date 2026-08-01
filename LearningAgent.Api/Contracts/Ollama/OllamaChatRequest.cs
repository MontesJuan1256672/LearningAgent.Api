namespace LearningAgent.Api.Contracts.Ollama;

public class OllamaChatRequest
{
    public string Model { get; set; } = string.Empty;

    public List<OllamaMessage> Messages { get; set; } = [];

    public bool Stream { get; set; } = false;
}