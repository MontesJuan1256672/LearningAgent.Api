using LearningAgent.Api.Models.Chat;

namespace LearningAgent.Api.Services.Memory
{
    public class ConversationMemory : IConversationMemory
    {
        private readonly List<ConversationMessage> _messages = new();

        public IReadOnlyList<ConversationMessage> GetMessages()
        {
            return _messages;
        }

        public void AddUserMessage(string message)
        {
            _messages.Add(new ConversationMessage
            {
                Role = "user",
                Content = message
            });
        }

        public void AddAssistantMessage(string message)
        {
            _messages.Add(new ConversationMessage
            {
                Role = "assistant",
                Content = message
            });
        }

        public void Clear()
        {
            _messages.Clear();
        }
    }
}
