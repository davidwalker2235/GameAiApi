using System.Text.Json;

namespace GameAiApi.Contracts;

public sealed class ChatResponse
{
    public string Raw { get; init; } = string.Empty;

    public JsonElement? Structured { get; init; }
}
