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
            _httpClient.Timeout = TimeSpan.FromMinutes(5);//esto solo es para diagnóstico, luego se puede quitar o ajustar según sea necesario
        }

        public async Task<ChatResult> GetResponseAsync(IEnumerable<ConversationMessage> messages, bool enableTools)
        {
            var tools = enableTools
                ? _toolRegistry.GetTools()
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
                    .ToList()
                : [];

            var request = new OllamaChatRequest
            {
                Model = _options.Model,
                Stream = false,

                Messages = messages
                    .Select(m => new OllamaMessage
                    {
                        Role = m.Role,
                        Content = m.Content,
                        ToolName = m.Role == "tool" ? m.ToolName : null,
                        ToolCallId = m.Role == "tool" ? m.ToolCallId : null,
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

            //agregado temporalmente para depuración de mensajes y herramientas
            foreach (var mensaje in request.Messages)
            {
                Console.WriteLine(
                    $"Role={mensaje.Role}, " +
                    $"ContentChars={mensaje.Content?.Length ?? 0}, " +
                    $"ToolCalls={mensaje.ToolCalls?.Count ?? 0}");
            }

            foreach (var (message, index) in request.Messages.Select((m, i) => (m, i)))
            {
                Console.WriteLine(
                    $"Mensaje {index + 1}: " +
                    $"Role={message.Role}, " +
                    $"ContentChars={message.Content?.Length ?? 0}, " +
                    $"ContentBytes={Encoding.UTF8.GetByteCount(message.Content ?? string.Empty)}, " +
                    $"ToolCalls={message.ToolCalls?.Count ?? 0}");

                Console.WriteLine(
                    $"Contenido: {(message.Content ?? string.Empty)[..Math.Min((message.Content ?? string.Empty).Length, 300)]}");
            }

            Console.WriteLine($"Tools disponibles: {request.Tools?.Count ?? 0}");

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Console.WriteLine($"Inicio llamada Ollama: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"Modelo: {_options.Model}");
            Console.WriteLine($"Mensajes: {request.Messages.Count}");
            Console.WriteLine($"Tamaño JSON: {Encoding.UTF8.GetByteCount(json)} bytes");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("/api/chat", content);                   
                Console.WriteLine($"Respuesta HTTP recibida: {stopwatch.Elapsed.TotalSeconds:F2} s");
                response.EnsureSuccessStatusCode();
                string responseJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Tiempo total hasta leer respuesta: {stopwatch.Elapsed.TotalSeconds:F2} s");

                Console.WriteLine("Respuesta RAW de Ollama:");
                Console.WriteLine(responseJson);

                var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson, JsonOptions);

                Console.WriteLine($"Ollama Total: {ollamaResponse?.TotalDuration / 1_000_000_000.0:F2} s");
                Console.WriteLine($"Ollama Load: {ollamaResponse?.LoadDuration / 1_000_000_000.0:F2} s");
                Console.WriteLine($"Ollama Prompt Eval: {ollamaResponse?.PromptEvalDuration / 1_000_000_000.0:F2} s");
                Console.WriteLine($"Ollama Eval: {ollamaResponse?.EvalDuration / 1_000_000_000.0:F2} s");


                return new ChatResult
                {
                    Content = ollamaResponse?.Message.Content ?? "No se recibió respuesta.",

                    ToolCalls = ollamaResponse?.Message?.ToolCalls?
                        .Select(toolCall => new ToolCall
                        {
                            Id = toolCall.Id,
                            Name = toolCall.Function.Name,
                            Arguments = toolCall.Function.Arguments.GetRawText()
                        })
                        .ToList() ?? []
                };

            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Timeout/cancelación Ollama tras {stopwatch.Elapsed.TotalSeconds:F2} s");
                Console.WriteLine($"HttpClient.Timeout: {_httpClient.Timeout}");
                Console.WriteLine(ex.ToString());

                throw;
            }
        }
    }
}
