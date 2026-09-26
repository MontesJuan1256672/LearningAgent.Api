using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Models.Conversation;
using LearningAgent.Api.Services.Chat;
using LearningAgent.Api.Services.Conversation;
using LearningAgent.Api.Services.Memory;
using LearningAgent.Api.Services.Prompts;
using LearningAgent.Api.Services.Tools;

namespace LearningAgent.Api.Services.Agent;

public class AgentService : IAgentService
{
    private readonly IChatService _chatService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IMemoryService _memoryService;
    private readonly IToolRegistry _toolRegistry;

    public AgentService(
        IChatService chatService,
        IPromptBuilder promptBuilder,
        IMemoryService memoryService,
        IToolRegistry toolRegistry)
    {
        _chatService = chatService;
        _promptBuilder = promptBuilder;
        _memoryService = memoryService;
        _toolRegistry = toolRegistry;
    }

    public async Task<string> ProcessAsync(Guid conversationId, string message)
    {
        return await _memoryService.ExecuteAsync(conversationId, async () =>
        {
            var context = _memoryService.GetOrCreate(conversationId);

            context.Messages.Add(new ConversationMessage
            {
                Role = "user",
                Content = message
            });

            var messages = _promptBuilder.Build(context);

            var chatResult = await _chatService.GetResponseAsync(messages);

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

                chatResult = await _chatService.GetResponseAsync(messages);                
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
}