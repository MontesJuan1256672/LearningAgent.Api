using LearningAgent.Api.Models.Conversation;
using LearningAgent.Api.Services.Conversation;

namespace LearningAgent.Api.Services.Memory;

public class MemoryService : IMemoryService
{
    private readonly IConversationContextFactory _contextFactory;
    private readonly IConversationStore _conversationStore;

    public MemoryService(IConversationContextFactory contextFactory, IConversationStore conversationStore)
    {
        _contextFactory = contextFactory;
        _conversationStore = conversationStore;
    }

    public ConversationContext GetOrCreate(Guid conversationId)
    {
        var context = _conversationStore.Get(conversationId);

        if(context is not null)
        {
            return context;
        }

        context = _contextFactory.Create(conversationId);

        _conversationStore.Save(context);


        return context;
    }

    public void Save(ConversationContext context)
    {
        _conversationStore.Save(context);
    }
}