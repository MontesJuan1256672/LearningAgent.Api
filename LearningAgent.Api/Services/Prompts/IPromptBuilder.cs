using LearningAgent.Api.Models.Chat;

namespace LearningAgent.Api.Services.Prompts;

public interface IPromptBuilder
{
    IEnumerable<ConversationMessage> Build(string userMessage);
}