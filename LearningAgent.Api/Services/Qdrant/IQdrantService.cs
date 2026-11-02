
using Qdrant.Client.Grpc;

namespace LearningAgent.Api.Services.Qdrant;

public interface IQdrantService
{
    Task<IReadOnlyList<string>> ListCollectionsAsync();

    Task CreateCollectionAsync();

    Task UpsertPointsAsync(IReadOnlyList<PointStruct> points);

    Task<IReadOnlyList<ScoredPoint>> SearchPointsAsync(float[] vector, ulong limit);        
       
}
