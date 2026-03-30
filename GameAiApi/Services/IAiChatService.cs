using GameAiApi.Contracts;

namespace GameAiApi.Services;

public interface IAiChatService
{
    Task<ContextInfo> UploadContextAsync(string name, Stream markdownStream, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContextInfo>> ListContextsAsync(CancellationToken cancellationToken);

    Task<ContextInfo> GetContextAsync(string name, CancellationToken cancellationToken);

    Task DeleteContextAsync(string name, CancellationToken cancellationToken);

    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken);
}
