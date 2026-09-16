using LearningAgent.Api.Services.Tools;

namespace LearningAgent.Api.Models.Chat;

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public IEnumerable<ToolCall> ToolCalls { get; set; } = [];
}
