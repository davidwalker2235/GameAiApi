namespace GameAiApi.Options;

public sealed class AzureFoundryOptions
{
    public const string SectionName = "AzureFoundry";

    public string Endpoint { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
