namespace GameAiApi.Contracts;

public sealed class UploadContextRequest
{
    public IFormFile File { get; set; } = null!;
}
