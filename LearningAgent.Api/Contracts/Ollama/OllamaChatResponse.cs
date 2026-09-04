using System.Text.Json.Serialization;

namespace LearningAgent.Api.Contracts.Ollama
{
    public class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage Message { get; set; } = new();

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }

        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
    }
}
