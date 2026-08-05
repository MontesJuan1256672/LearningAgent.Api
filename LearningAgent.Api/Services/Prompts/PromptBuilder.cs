using LearningAgent.Api.Models.Chat;

namespace LearningAgent.Api.Services.Prompts;

public class PromptBuilder : IPromptBuilder
{
    private readonly ISystemPromptProvider _systemPromptProvider;

    public PromptBuilder(ISystemPromptProvider systemPromptProvider)
    {
        _systemPromptProvider = systemPromptProvider;
    }

    public IEnumerable<ConversationMessage> Build(string userMessage)
    {
        return new List<ConversationMessage>
        {
            new()
            {
                Role = "system",
                Content = _systemPromptProvider.GetSystemPrompt()
            },

            new()
            {
                Role = "user",
                Content = userMessage
            }
        };
    }
}






