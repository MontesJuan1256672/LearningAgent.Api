using LearningAgent.Api.Models.Chat;

namespace LearningAgent.Api.Services.Memory
{
    public interface IConversationMemory
    {
        IReadOnlyList<ConversationMessage> GetMessages();

        void AddUserMessage(string message);

        void AddAssistantMessage(string message);

        void Clear();
    }
}
