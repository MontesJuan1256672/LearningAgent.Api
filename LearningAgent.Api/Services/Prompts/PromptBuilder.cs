using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Prompts;

public class PromptBuilder : IPromptBuilder
{

    public IEnumerable<ConversationMessage> Build(ConversationContext context)
    {
        const int maxHistoryMessages = 10;

        var messages = new List<ConversationMessage>
        {
            new()
            {
                Role = "system",
                Content = context.SystemPrompt
            }
        };

        var history = context.Messages.TakeLast(maxHistoryMessages);

        messages.AddRange(history);

        return messages;
    }

}


