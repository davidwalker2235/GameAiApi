using GameAiApi.Contracts;

namespace GameAiApi.Services;

public interface IAiChatService
{
    Task<ContextStatusResponse> UploadContextAsync(Stream markdownStream, CancellationToken cancellationToken);

    Task<ContextStatusResponse> GetContextStatusAsync(CancellationToken cancellationToken);

    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken);
}
