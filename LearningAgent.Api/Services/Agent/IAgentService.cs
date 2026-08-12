using LearningAgent.Api.Models.Conversation;

namespace LearningAgent.Api.Services.Agent
{
    public interface IAgentService
    {
        Task<string> ProcessAsync(Guid conversationId, string message);
    }
}
