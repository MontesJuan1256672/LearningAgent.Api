using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Conversation;

public interface IConversationContextFactory
{
    ConversationContext Create(Guid conversationId);
}