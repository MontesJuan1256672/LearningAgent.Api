using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Prompts;

public class PromptBuilder : IPromptBuilder
{
    private readonly ISystemPromptProvider _systemPromptProvider;

    public PromptBuilder(ISystemPromptProvider systemPromptProvider)
    {
        _systemPromptProvider = systemPromptProvider;
    }

    public IEnumerable<ConversationMessage> Build(ConversationContext contex)
    {
        var messages = new List<ConversationMessage>
       {
           new()
           {
               Role = "System",
               Content = contex.SystemPrompt
           }
       };

        messages.AddRange(contex.Messages);
        return messages;
    }
}






