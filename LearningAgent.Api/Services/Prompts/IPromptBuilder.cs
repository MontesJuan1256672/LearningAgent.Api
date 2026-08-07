using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Prompts;

public interface IPromptBuilder
{
    IEnumerable<ConversationMessage> Build(ConversationContext contex);
}