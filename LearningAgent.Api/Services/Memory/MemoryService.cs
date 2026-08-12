using LearningAgent.Api.Models.Conversation;
using LearningAgent.Api.Services.Conversation;

namespace LearningAgent.Api.Services.Memory;

public class MemoryService : IMemoryService
{
    private readonly Dictionary<Guid, ConversationContext> _conversations = [];
    private readonly IConversationContextFactory _contextFactory;

    public MemoryService(IConversationContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public ConversationContext GetOrCreate(Guid conversationId)
    {
        if (_conversations.TryGetValue(conversationId, out var context))
        {
            return context;
        }

        context = _contextFactory.Create(conversationId);

        _conversations[conversationId] = context;

        return context;
    }

    public void Save(ConversationContext context)
    {
        _conversations[context.ConversationId] = context;
    }
}