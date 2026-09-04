namespace LearningAgent.Api.Services.Tool
{
    public class CalculatorTool : ITool
    {
        public string Name => "calculator";

        public string Description => "Realiza operaciones matemáticas simples.";

        public Task<string> ExecuteAsync(string arguments)
        {
            return Task.FromResult($"CalculatorTool recibió: {arguments}");
        }
    }
}
