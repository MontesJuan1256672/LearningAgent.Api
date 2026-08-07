
using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Models.Conversation;
using LearningAgent.Api.Services.Chat;
using LearningAgent.Api.Services.Conversation;
using LearningAgent.Api.Services.Prompts;

namespace LearningAgent.Api.Services.Agent
{
    public class AgentService : IAgentService
    {
        private readonly IChatService _chatService;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IConversationContextFactory _contextFactory;

        public AgentService(IChatService chatService, IPromptBuilder promptBuilder, IConversationContextFactory contextFactory)            
        {
            _chatService = chatService;
            _promptBuilder = promptBuilder;
            _contextFactory = contextFactory;
        }

        public async Task<string> ProcessAsync(string message)
        {
            var context = _contextFactory.Create();

            context.Messages.Add(new ConversationMessage
            {
                Role = "user",
                Content = message
            });

            var messages = _promptBuilder.Build(context);

            var response = await _chatService.GetResponseAsync(messages);

            context.Messages.Add(new ConversationMessage
            {
                Role = "assistant",
                Content = response
            });

            return response;
        }
    }
}
