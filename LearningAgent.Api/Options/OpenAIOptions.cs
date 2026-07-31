namespace LearningAgent.Api.Options;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5";
}