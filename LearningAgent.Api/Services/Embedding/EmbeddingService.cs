using System.Net.Http.Json;

namespace LearningAgent.Api.Services.Embedding
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;

        public EmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<float[]> GenerateAsync(string text)
        {
            var request = new
            {
                model = "nomic-embed-text",
                input = text
            };

            var response = await _httpClient.PostAsJsonAsync("api/embed", request);
                
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();

            return result?.Embeddings.FirstOrDefault()
                   ?? throw new InvalidOperationException(
                       "Ollama no devolvió un embedding.");
        }

        private class EmbeddingResponse
        {
            public List<float[]> Embeddings { get; set; } = [];
        }
    }
}