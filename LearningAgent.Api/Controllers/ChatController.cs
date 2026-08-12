using LearningAgent.Api.Dtos;
using LearningAgent.Api.Services;
using LearningAgent.Api.Services.Agent;
using Microsoft.AspNetCore.Mvc;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agentService;

    public ChatController(IAgentService agentService)
    {
        _agentService = agentService;
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
}