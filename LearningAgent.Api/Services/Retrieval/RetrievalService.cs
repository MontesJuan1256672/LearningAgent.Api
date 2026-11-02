
using System.Text;
using LearningAgent.Api.Services.Embedding;
using LearningAgent.Api.Services.Qdrant;

namespace LearningAgent.Api.Services.Retrieval;

public class RetrievalService : IRetrievalService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;

    public RetrievalService(
        IEmbeddingService embeddingService,
        IQdrantService qdrantService)
    {
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
    }

    public async Task<string> RetrieveContextAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // 1. Generar el embedding de la pregunta.
        var embedding = await _embeddingService.GenerateAsync(query);

        // 2. Recuperar los cinco fragmentos más similares.
        var results = await _qdrantService.SearchPointsAsync(embedding, 5);
            
        if (results.Count == 0)
            return string.Empty;

        // 3. Construir el contexto documental.
        var context = new StringBuilder();

        foreach (var point in results)
        {
            string fileName = point.Payload.TryGetValue(
                "fileName", out var file)
                    ? file.StringValue
                    : "Documento desconocido";

            long chunkIndex = point.Payload.TryGetValue(
                "chunkIndex", out var chunk)
                    ? chunk.IntegerValue
                    : -1;

            string text = point.Payload.TryGetValue(
                "text", out var content)
                    ? content.StringValue
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(text))
                continue;

            context.AppendLine($"Documento: {fileName}");
            context.AppendLine($"Fragmento: {chunkIndex}");
            context.AppendLine($"Relevancia: {point.Score:F3}");
            context.AppendLine(text);
            context.AppendLine();
        }

        return context.ToString().Trim();
    }
}
