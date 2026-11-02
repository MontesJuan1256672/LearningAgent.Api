namespace LearningAgent.Api.Services.Embedding
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateAsync(string text);
    }
}