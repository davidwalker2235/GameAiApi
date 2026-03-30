using System.ComponentModel.DataAnnotations;

namespace GameAiApi.Contracts;

public sealed class ChatRequest
{
    [Required]
    public string Prompt { get; init; } = string.Empty;

    [Required]
    public string ContextName { get; init; } = string.Empty;

    public IReadOnlyList<RecentCard> RecentCards { get; init; } = [];
}

public sealed class RecentCard
{
    public string? Name { get; init; }
    public string? Role { get; init; }
    public string? Type { get; init; }
    public string? Gender { get; init; }
    public string? Theme { get; init; }
}
