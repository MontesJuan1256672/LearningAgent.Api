using LearningAgent.Api.Dtos;
using LearningAgent.Api.Services;
using LearningAgent.Api.Services.Agent;
using LearningAgent.Api.Services.Tools;
using Microsoft.AspNetCore.Mvc;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IToolRegistry _toolRegistry;

    public ChatController(IAgentService agentService, IToolRegistry toolRegistry)
    {
        _agentService = agentService;
        _toolRegistry = toolRegistry;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
    {
        //string response = await _chatService.GetResponseAsync(request.Message);
        string response = await _agentService.ProcessAsync(request.ConversationId, request.Message);

        return Ok(new ChatResponse
        {
            Response = response
        });
    }

    [HttpGet("test-tool/{name}")]
    public async Task<IActionResult> TestTool(string name)
    {
        var tool = _toolRegistry.GetTool(name);

        if (tool is null)
            return NotFound($"No se encontró la herramienta '{name}'.");

        var result = await tool.ExecuteAsync("10 + 20");

        return Ok(new
        {
            tool.Name,
            tool.Description,
            result
        });
    }
}