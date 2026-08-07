namespace LearningAgent.Api.Models.Conversation;

using LearningAgent.Api.Models.Chat;

public class ConversationContext
{
    public Guid ConversationId { get; init; } = Guid.NewGuid();

    public List<ConversationMessage> Messages { get; } = [];

    public string SystemPrompt { get; set; } = string.Empty;
}

