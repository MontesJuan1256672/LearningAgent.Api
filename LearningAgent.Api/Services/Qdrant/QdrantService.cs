using LearningAgent.Api.Options;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace LearningAgent.Api.Services.Qdrant;

public class QdrantService : IQdrantService
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantService(
        QdrantClient client,
        IOptions<QdrantOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<string>> ListCollectionsAsync()
    {
        return await _client.ListCollectionsAsync();
    }

    public async Task CreateCollectionAsync()
    {
        await _client.CreateCollectionAsync(
            collectionName: _options.CollectionName,
            vectorsConfig: new VectorParams
            {
                Size = _options.VectorSize,
                Distance = Distance.Cosine
            });
    }

    public async Task UpsertPointsAsync(IReadOnlyList<PointStruct> points)
    {
        await _client.UpsertAsync(_options.CollectionName, points);
    }

    public async Task<IReadOnlyList<ScoredPoint>> SearchPointsAsync(
    float[] vector,
    ulong limit)
    {
        return await _client.QueryAsync(
        collectionName: _options.CollectionName,
        query: vector,
        payloadSelector: true,
        limit: limit);
    }
}

