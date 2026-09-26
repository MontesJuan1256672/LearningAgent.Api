using LearningAgent.Api.Contracts.Ollama;
using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Options;
using LearningAgent.Api.Services.Tools;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace LearningAgent.Api.Services.Chat
{
    public class OllamaService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;
        private readonly IToolRegistry _toolRegistry;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public OllamaService(
            IHttpClientFactory httpClientFactory, 
            IOptions<OllamaOptions> options, 
            IToolRegistry toolRegistry)
        {
            _httpClient = httpClientFactory.CreateClient();
            _options = options.Value;
            _toolRegistry = toolRegistry;
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        public async Task<ChatResult> GetResponseAsync(IEnumerable<ConversationMessage> messages)
        {
            var tools = _toolRegistry
                .GetTools()
                .Select(tool => new OllamaTool
                {
                    Type = "function",

                    Function = new OllamaToolFunction
                    {
                        Name = tool.Definition.Name,
                        Description = tool.Definition.Description,
                        Parameters = tool.Definition.Parameters
                    }
                })
                .ToList();

            var request = new OllamaChatRequest
            {
                Model = _options.Model,
                Stream = false,

                Messages = messages
                    .Select(m => new OllamaMessage
                    {
                        Role = m.Role,
                        Content = m.Content,
                        ToolName = m.ToolName,
                        ToolCallId = m.ToolCallId,
                        ToolCalls = m.ToolCalls
                            .Select(toolCall => new OllamaToolCall
                            {
                                Id = toolCall.Id,
                                Function = new OllamaToolCallFunction
                                {
                                    Name = toolCall.Name,
                                    Arguments = JsonSerializer.Deserialize<JsonElement>(
                                        toolCall.Arguments)
                                }
                            })
                            .ToList()
                    })
                    .ToList(),

                Tools = tools
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
            Console.WriteLine("Respuesta RAW de Ollama:");
            Console.WriteLine(responseJson);

            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson, JsonOptions);

            Console.WriteLine($"Ollama Total: {ollamaResponse?.TotalDuration / 1_000_000_000.0:F2} s");
            Console.WriteLine($"Ollama Load: {ollamaResponse?.LoadDuration / 1_000_000_000.0:F2} s");
            Console.WriteLine($"Ollama Prompt Eval: {ollamaResponse?.PromptEvalDuration / 1_000_000_000.0:F2} s");
            Console.WriteLine($"Ollama Eval: {ollamaResponse?.EvalDuration / 1_000_000_000.0:F2} s");

            //return ollamaResponse?.Message.Content?? "No se recibió respuesta.";
            return new ChatResult
            {
                Content = ollamaResponse?.Message.Content ?? "No se recibió respuesta.",
                ToolCalls = ollamaResponse?.Message.ToolCalls
                    .Select(toolCall => new ToolCall
                    {
                        Id = toolCall.Id,
                        Name = toolCall.Function.Name,
                        Arguments = toolCall.Function.Arguments.GetRawText()
                    })
                    ?? []
            };      
        }
    }
}
