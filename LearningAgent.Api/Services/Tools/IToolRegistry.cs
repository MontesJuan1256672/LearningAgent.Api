using LearningAgent.Api.Services.Tool;

namespace LearningAgent.Api.Services.Tools;

public interface IToolRegistry
{
    ITool? GetTool(string name);
}