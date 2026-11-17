using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Services.Chat;
using LearningAgent.Api.Services.Memory;
using LearningAgent.Api.Services.Prompts;
using LearningAgent.Api.Services.Tools;
using LearningAgent.Api.Services.Retrieval;

namespace LearningAgent.Api.Services.Agent;

public class AgentService : IAgentService
{
    private readonly IChatService _chatService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IMemoryService _memoryService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IRetrievalService _retrievalService;

    public AgentService(
        IChatService chatService,
        IPromptBuilder promptBuilder,
        IMemoryService memoryService,
        IToolRegistry toolRegistry,
        IRetrievalService retrievalService)
    {
        _chatService = chatService;
        _promptBuilder = promptBuilder;
        _memoryService = memoryService;
        _toolRegistry = toolRegistry;
        _retrievalService = retrievalService;
    }

    public async Task<string> ProcessAsync(Guid conversationId, string message)
    {
        return await _memoryService.ExecuteAsync(conversationId, async () =>
        {
            var context = _memoryService.GetOrCreate(conversationId);

            Console.WriteLine($"Guid: {conversationId}");
            Console.WriteLine($"Mensajes recuperados: {context.Messages.Count}");
            Console.WriteLine($"Caracteres del historial: {context.Messages.Sum(m => m.Content?.Length ?? 0)}");

            string retrievedContext = string.Empty;

            if (!EsOperacionMatematicaSimple(message))
            {
                retrievedContext = await _retrievalService.RetrieveContextAsync(message);     
            }

            var augmentedMessage = string.IsNullOrWhiteSpace(retrievedContext)    
                ? message
                : $"""
                    Pregunta del usuario:
                    {message}

                    Contexto documental recuperado:
                    {retrievedContext}

                    Instrucciones:
                    Responde utilizando el contexto documental cuando sea pertinente.
                    Si el contexto no contiene la respuesta, indícalo claramente.
                    No inventes información ni afirmes que el contexto dice algo que no contiene.
                    """;

            context.Messages.Add(new ConversationMessage
            {
                Role = "user",
                Content = augmentedMessage
            });

            var messages = _promptBuilder.Build(context);

            Console.WriteLine($"Mensajes enviados a Ollama: {messages.Count()}");

            bool habilitarHerramientas = EsOperacionMatematicaSimple(message);

            var chatResult = await _chatService.GetResponseAsync(messages, habilitarHerramientas);

            const int maxToolIterations = 5;

            int toolIteration = 0;

            while (chatResult.ToolCalls.Any() && toolIteration < maxToolIterations)                  
            {
                toolIteration++;

                context.Messages.Add(new ConversationMessage
                {
                    Role = "assistant",
                    Content = chatResult.Content,
                    ToolCalls = chatResult.ToolCalls
                });

                foreach (var toolCall in chatResult.ToolCalls)
                {
                    var tool = _toolRegistry.GetTool(toolCall.Name);

                    if (tool is null)
                    {
                        context.Messages.Add(new ConversationMessage
                        {
                            Role = "tool",
                            ToolName = toolCall.Name,
                            Content = $"Tool '{toolCall.Name}' not found."
                        });

                        continue;
                    }

                    var toolResult = await tool.ExecuteAsync(toolCall.Arguments);
                       
                    context.Messages.Add(new ConversationMessage
                    {
                        Role = "tool",
                        ToolName = toolCall.Name,
                        ToolCallId = toolCall.Id,
                        Content = toolResult
                    });
                }

                messages = _promptBuilder.Build(context);

                chatResult = await _chatService.GetResponseAsync(messages, habilitarHerramientas);                
            }

            context.Messages.Add(new ConversationMessage
            {
                Role = "assistant",
                Content = chatResult.Content,
                ToolCalls = chatResult.ToolCalls
            });

            _memoryService.Save(context);

            return chatResult.Content;
        });
    }

    private static bool EsOperacionMatematicaSimple(string message)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            message,
            @"^\s*¿?\s*(cuánto es\s+)?\d+(?:\s*[+\-*/]\s*\d+)+\s*\??\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}