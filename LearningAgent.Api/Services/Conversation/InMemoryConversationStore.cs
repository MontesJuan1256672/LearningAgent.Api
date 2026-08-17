using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Conversation;

public class InMemoryConversationStore : IConversationStore
{
    private readonly Dictionary<Guid, ConversationContext> _conversations = [];

    public ConversationContext? Get(Guid conversationId)
    {
        _conversations.TryGetValue(conversationId, out var context);

        return context;
    }

    public void Save(ConversationContext context)
    {
        _conversations[context.ConversationId] = context;
    }
}