namespace LearningAgent.Api.Services.Agent
{
    public interface IAgentService
    {
        Task<string> ProcessAsync(string message);
    }
}
