using LearningAgent.Api.Services.Tools;

namespace LearningAgent.Api.Services.Tool
{
    public interface ITool
    {
        ToolDefinition Definition { get; }

        Task<string> ExecuteAsync(string arguments);
    }
}
