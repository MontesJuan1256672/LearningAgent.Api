using LearningAgent.Api.Models.Chat;

namespace LearningAgent.Api.Services.Chat;

public interface IChatService
{
    //Task<string> GetResponseAsync(string message);
    Task<string> GetResponseAsync(IEnumerable<ConversationMessage> messages);
}
