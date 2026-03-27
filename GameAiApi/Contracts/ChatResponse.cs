using System.Text.Json.Serialization;

namespace GameAiApi.Contracts;

public sealed class ChatResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("situation")]
    public string Situation { get; set; } = string.Empty;

    [JsonPropertyName("left_option")]
    public string LeftOption { get; set; } = string.Empty;

    [JsonPropertyName("right_option")]
    public string RightOption { get; set; } = string.Empty;

    [JsonPropertyName("effects")]
    public EffectsResponse? Effects { get; set; }
}

public sealed class EffectsResponse
{
    [JsonPropertyName("left")]
    public Dictionary<string, int>? Left { get; set; }

    [JsonPropertyName("right")]
    public Dictionary<string, int>? Right { get; set; }
}
