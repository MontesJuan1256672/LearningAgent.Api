namespace LearningAgent.Api.Contracts.Ollama
{
    public class OllamaChatResponse
    {
        public OllamaMessage Message { get; set; } = new();
    }
}
