using LearningAgent.Api.Models.Chat;
using LearningAgent.Api.Options;        //nos permite leer appsettings.json "OpenAI"
using Microsoft.Extensions.Options;
using OpenAI.Chat;                      //cliente especializado para conversaciones

namespace LearningAgent.Api.Services.Chat
{
    public class OpenAIService : IChatService
    {
        private readonly ChatClient _chatClient;

        public OpenAIService(IOptions<OpenAIOptions> options)
        {
            _chatClient = new ChatClient(
                model: options.Value.Model,
                apiKey: options.Value.ApiKey);
        }

        public async Task<string> GetResponseAsync(IEnumerable<ConversationMessage> messages)
        {
            var openAiMessages = new List<OpenAI.Chat.ChatMessage>();

            foreach (var message in messages)
            {
                if (message.Role == "system")
                {
                    openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateSystemMessage(message.Content));
                }
                else if (message.Role == "assistant")
                {
                    openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(message.Content));
                }
                else
                {
                    openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateUserMessage(message.Content));
                }
            }

            ChatCompletion completion = await _chatClient.CompleteChatAsync(openAiMessages);
            return completion.Content[0].Text;
        }
    }
}

