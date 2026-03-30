namespace GameAiApi.Contracts;

public sealed class ContextStatusResponse
{
    public bool HasContext { get; init; }

    public string? Version { get; init; }
}

public sealed class ContextInfo
{
    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public DateTimeOffset UploadedAt { get; init; }

    public long SizeBytes { get; init; }
}
