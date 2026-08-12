using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Memory;

public interface IMemoryService
{
    ConversationContext GetOrCreate(Guid conversationId);

    void Save(ConversationContext context);
}