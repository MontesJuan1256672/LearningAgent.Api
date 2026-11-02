using LearningAgent.Api.Models.Chat;

namespace LearningAgent.Api.Services.Chat;

public interface IChatService
{
    Task<ChatResult> GetResponseAsync(IEnumerable<ConversationMessage> messages);
}
