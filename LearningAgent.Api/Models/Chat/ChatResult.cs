using LearningAgent.Api.Services.Tools;

namespace LearningAgent.Api.Models.Chat;

public class ChatResult
{
    public string Content { get; set; } = string.Empty;

    public IEnumerable<ToolCall> ToolCalls { get; set; } = [];
}