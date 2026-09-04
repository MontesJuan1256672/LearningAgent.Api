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

        public async Task<ChatResult> GetResponseAsync(IEnumerable<ConversationMessage> messages)
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
            Console.WriteLine($"Mensajes enviados: {request.Messages.Count}");
            Console.WriteLine($"Tamaño JSON: {Encoding.UTF8.GetByteCount(json)} bytes");

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            HttpResponseMessage response = await _httpClient.PostAsync("/api/chat", content);

            stopwatch.Stop();

            Console.WriteLine($"Tiempo HTTP: {stopwatch.ElapsedMilliseconds} ms");

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();

            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson, JsonOptions);

            Console.WriteLine($"Ollama Total: {ollamaResponse?.TotalDuration / 1_000_000_000.0:F2} s");
            Console.WriteLine($"Ollama Load: {ollamaResponse?.LoadDuration / 1_000_000_000.0:F2} s");
            Console.WriteLine($"Ollama Prompt Eval: {ollamaResponse?.PromptEvalDuration / 1_000_000_000.0:F2} s");
            Console.WriteLine($"Ollama Eval: {ollamaResponse?.EvalDuration / 1_000_000_000.0:F2} s");

            //return ollamaResponse?.Message.Content?? "No se recibió respuesta.";
            return new ChatResult
            {
                Content = ollamaResponse?.Message.Content ?? "No se recibió respuesta"
            };
        }
    }
}
