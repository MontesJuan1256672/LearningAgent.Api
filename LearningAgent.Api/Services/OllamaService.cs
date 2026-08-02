using System.Text;
using System.Text.Json;
using LearningAgent.Api.Options;
using Microsoft.Extensions.Options;
using LearningAgent.Api.Contracts.Ollama;

namespace LearningAgent.Api.Services
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

        public async Task<string> GetResponseAsync(string message)
        {
            var request = new OllamaChatRequest
            {
                Model = _options.Model,
                Stream = false,
                Messages =
                [
                    new OllamaMessage
                    {
                         Role = "user",
                         Content = message
                    }
                ]
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
