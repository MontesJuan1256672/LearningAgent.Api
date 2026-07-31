using LearningAgent.Api.Options;        //nos permite leer appsettings.json "OpenAI"
using Microsoft.Extensions.Options;
using OpenAI.Chat;                      //cliente especializado para conversaciones

namespace LearningAgent.Api.Services
{
    public class OpenAIService
    {
        private readonly ChatClient _chatClient;

        public OpenAIService(IOptions<OpenAIOptions> options)
        {
            _chatClient = new ChatClient(
                model: options.Value.Model,
                apiKey: options.Value.ApiKey);
        }
    }
}
