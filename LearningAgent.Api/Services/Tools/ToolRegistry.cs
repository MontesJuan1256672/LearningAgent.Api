using LearningAgent.Api.Services.Tool;

namespace LearningAgent.Api.Services.Tools;

public class ToolRegistry : IToolRegistry
{
    private readonly IEnumerable<ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools;
    }

    public ITool? GetTool(string name)
    {
        return _tools.FirstOrDefault(
            tool => tool.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase));
    }
}