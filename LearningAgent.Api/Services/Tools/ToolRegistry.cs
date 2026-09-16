using LearningAgent.Api.Services.Tool;

namespace LearningAgent.Api.Services.Tools;

public class ToolRegistry : IToolRegistry
{
    private readonly IEnumerable<ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools;
    }

    public IEnumerable<ITool> GetTools()
    {
        return _tools;
    }

    public ITool? GetTool(string name)
    {
        return _tools.FirstOrDefault(
            tool => tool.Definition.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase));
    }
}