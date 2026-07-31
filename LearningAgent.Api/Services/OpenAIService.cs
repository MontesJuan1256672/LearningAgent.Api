using LearningAgent.Api.Options;        //nos permite leer appsettings.json "OpenAI"
using Microsoft.Extensions.Options;
using OpenAI.Chat;                      //cliente especializado para conversaciones

namespace LearningAgent.Api.Services
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

        public async Task<string> GetResponseAsync(string message)
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(message);
            return completion.Content[0].Text;
        }
    }
}
