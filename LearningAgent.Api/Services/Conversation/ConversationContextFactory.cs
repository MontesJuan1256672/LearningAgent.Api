using LearningAgent.Api.Models.Conversation;
using LearningAgent.Api.Services.Prompts;

namespace LearningAgent.Api.Services.Conversation;

public class ConversationContextFactory : IConversationContextFactory
{
    private readonly ISystemPromptProvider _systemPromptProvider;

    public ConversationContextFactory(
        ISystemPromptProvider systemPromptProvider)
    {
        _systemPromptProvider = systemPromptProvider;
    }

    public ConversationContext Create()
    {
        return new ConversationContext
        {
            SystemPrompt = _systemPromptProvider.GetSystemPrompt()
        };
    }
}