using System.Text;
using System.Text.Json;
using LearningAgent.Api.Options;
using Microsoft.Extensions.Options;

namespace LearningAgent.Api.Services
{
    public class OllamaService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;

        public OllamaService(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient();
            _options = options.Value;
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        public Task<string> GetResponseAsync(string message)
        {
            throw new NotImplementedException();
        }
    }
}
