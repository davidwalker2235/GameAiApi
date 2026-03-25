using System.ComponentModel.DataAnnotations;

namespace GameAiApi.Contracts;

public sealed class ChatRequest
{
    [Required]
    public string SessionId { get; init; } = string.Empty;

    [Required]
    public string Prompt { get; init; } = string.Empty;
}
