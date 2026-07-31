using LearningAgent.Api.Dtos;
using LearningAgent.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
    {
        string response = await _chatService.GetResponseAsync(request.Message);

        return Ok(new ChatResponse
        {
            Response = response
        });
    }
}