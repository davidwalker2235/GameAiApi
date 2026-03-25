namespace GameAiApi.Contracts;

public sealed class ContextStatusResponse
{
    public bool HasContext { get; init; }

    public string? Version { get; init; }
}
