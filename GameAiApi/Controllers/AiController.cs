using GameAiApi.Contracts;
using GameAiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameAiApi.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AiController : ControllerBase
{
    private readonly IAiChatService _aiChatService;

    public AiController(IAiChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    [HttpGet("context/status")]
    public async Task<ActionResult<ContextStatusResponse>> GetContextStatus(CancellationToken cancellationToken)
    {
        var status = await _aiChatService.GetContextStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("context")]
    [RequestSizeLimit(1024 * 1024)]
    public async Task<ActionResult<ContextStatusResponse>> UploadContext([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("El archivo está vacío.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".md", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Solo se permite un archivo Markdown (.md).");
        }

        await using var stream = file.OpenReadStream();
        var response = await _aiChatService.UploadContextAsync(stream, cancellationToken);
        return Ok(response);
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var response = await _aiChatService.ChatAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
