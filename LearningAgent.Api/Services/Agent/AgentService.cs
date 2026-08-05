
using LearningAgent.Api.Services.Chat;
using LearningAgent.Api.Services.Prompts;

namespace LearningAgent.Api.Services.Agent
{
    public class AgentService : IAgentService
    {
        private readonly IChatService _chatService;
        private readonly IPromptBuilder _promptBuilder;

        public AgentService(IChatService chatService, IPromptBuilder promptBuilder)            
        {
            _chatService = chatService;
            _promptBuilder = promptBuilder;
        }

        public async Task<string> ProcessAsync(string message)
        {
            var messages = _promptBuilder.Build(message);

            return await _chatService.GetResponseAsync(messages);
        }
    }
}
