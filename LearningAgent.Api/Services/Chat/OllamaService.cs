using LearningAgent.Api.Contracts.Ollama;
using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace LearningAgent.Api.Services.Chat
{
    public class OllamaService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public OllamaService(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient();
            _options = options.Value;
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        public async Task<string> GetResponseAsync(IEnumerable<ConversationMessage> messages)
        {
            var request = new OllamaChatRequest
            {
                Model = _options.Model,
                Stream = false,
                Messages = messages
                    .Select(m => new OllamaMessage
                    {
                        Role = m.Role,
                        Content = m.Content
                    })
                    .ToList()

            };

            string json = JsonSerializer.Serialize(request);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("/api/chat", content);

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();

            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson, JsonOptions);
               
            return ollamaResponse?.Message.Content ?? "No se recibió respueta.";
        }
    }
}
