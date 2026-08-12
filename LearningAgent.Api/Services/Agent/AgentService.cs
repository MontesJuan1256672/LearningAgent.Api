
using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Models.Conversation;
using LearningAgent.Api.Services.Chat;
using LearningAgent.Api.Services.Conversation;
using LearningAgent.Api.Services.Memory;
using LearningAgent.Api.Services.Prompts;

namespace LearningAgent.Api.Services.Agent
{
    public class AgentService : IAgentService
    {
        private readonly IChatService _chatService;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IMemoryService _memoryService;

        public AgentService(IChatService chatService, IPromptBuilder promptBuilder, IMemoryService memoryService)
        {
            _chatService = chatService;
            _promptBuilder = promptBuilder;
            _memoryService = memoryService;
        }

        public async Task<string> ProcessAsync(Guid conversationId, string message)
        {
            var context = _memoryService.GetOrCreate(conversationId);

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

            _memoryService.Save(context);

            return response;
        }
    }
}
