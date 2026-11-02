namespace LearningAgent.Api.Services.Retrieval;

public interface IRetrievalService
{
    Task<string> RetrieveContextAsync(string query);
}