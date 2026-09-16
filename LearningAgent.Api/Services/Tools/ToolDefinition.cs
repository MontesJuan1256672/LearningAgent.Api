using System.Text.Json;

namespace LearningAgent.Api.Services.Tools
{
    public class ToolDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public JsonElement Parameters { get; set; }
    }
}
