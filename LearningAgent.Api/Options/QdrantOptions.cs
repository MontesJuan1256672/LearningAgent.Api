
namespace LearningAgent.Api.Options;

public class QdrantOptions
{
    public string Host { get; set; } = "localhost";
    public int GrpcPort { get; set; } = 6334;
    public string CollectionName { get; set; } = "learningagent_documents";
    public ulong VectorSize { get; set; } = 768;
}
