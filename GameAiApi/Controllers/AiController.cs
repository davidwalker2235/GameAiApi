using GameAiApi.Contracts;
using GameAiApi.Domain;
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

    [HttpGet("contexts")]
    public async Task<ActionResult<IReadOnlyList<ContextInfo>>> ListContexts(CancellationToken cancellationToken)
    {
        var contexts = await _aiChatService.ListContextsAsync(cancellationToken);
        return Ok(contexts);
    }

    [HttpGet("contexts/{name}")]
    public async Task<ActionResult<ContextInfo>> GetContext(string name, CancellationToken cancellationToken)
    {
        try
        {
            var context = await _aiChatService.GetContextAsync(name, cancellationToken);
            return Ok(context);
        }
        catch (FileNotFoundException)
        {
            return NotFound($"Contexto '{name}' no encontrado.");
        }
    }

    [HttpPost("contexts")]
    [RequestSizeLimit(1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ContextInfo>> UploadContext([FromForm] UploadContextRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("El archivo está vacío.");

        if (!string.Equals(Path.GetExtension(request.File.FileName), ".md", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Solo se permite un archivo Markdown (.md).");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("El nombre del contexto es obligatorio.");

        try
        {
            await using var stream = request.File.OpenReadStream();
            var info = await _aiChatService.UploadContextAsync(request.Name, stream, cancellationToken);
            return Ok(info);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("contexts/{name}")]
    public async Task<IActionResult> DeleteContext(string name, CancellationToken cancellationToken)
    {
        try
        {
            await _aiChatService.DeleteContextAsync(name, cancellationToken);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound($"Contexto '{name}' no encontrado.");
        }
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var response = await _aiChatService.ChatAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("schema/options")]
    public ActionResult<object> GetSchemaOptions()
    {
        return Ok(new
        {
            type = AiResponseTypeCatalog.Types,
            role = AiResponseTypeCatalog.Roles,
            theme = AiResponseTypeCatalog.Themes
        });
    }
}
