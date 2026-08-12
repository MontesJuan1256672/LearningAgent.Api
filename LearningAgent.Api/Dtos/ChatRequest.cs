namespace LearningAgent.Api.Dtos;

public class ChatRequest
{
    public Guid ConversationId { get; set; }

    public string Message { get; set; } = string.Empty;
}