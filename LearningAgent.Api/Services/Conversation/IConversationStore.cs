using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Conversation;

public interface IConversationStore
{
    ConversationContext? Get(Guid conversationId);

    void Save(ConversationContext context);
}