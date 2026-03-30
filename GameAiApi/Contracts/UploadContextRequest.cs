using System.ComponentModel.DataAnnotations;

namespace GameAiApi.Contracts;

public sealed class UploadContextRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public IFormFile File { get; set; } = null!;
}
