using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.Inference;
using GameAiApi.Contracts;
using GameAiApi.Mappers;
using GameAiApi.Options;
using Microsoft.Extensions.Options;

namespace GameAiApi.Services;

public sealed class AzureFoundryChatService : IAiChatService
{
    private const string ContextsDirectory = "App_Data/contexts";
    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

    private readonly AzureFoundryOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, string> _contextsCache = new(StringComparer.OrdinalIgnoreCase);

    public AzureFoundryChatService(IOptions<AzureFoundryOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        Directory.CreateDirectory(ContextsDirectory);
        LoadAllContextsFromDisk();
    }

    public async Task<ContextInfo> UploadContextAsync(string name, Stream markdownStream, CancellationToken cancellationToken)
    {
        ValidateName(name);

        using var reader = new StreamReader(markdownStream, Encoding.UTF8, leaveOpen: true);
        var markdown = await reader.ReadToEndAsync(cancellationToken);

        var filePath = GetFilePath(name);
        await File.WriteAllTextAsync(filePath, markdown, cancellationToken);
        _contextsCache[name] = markdown;

        return BuildContextInfo(name, filePath);
    }

    public Task<IReadOnlyList<ContextInfo>> ListContextsAsync(CancellationToken cancellationToken)
    {
        var result = Directory
            .GetFiles(ContextsDirectory, "*.md")
            .Select(path => BuildContextInfo(Path.GetFileNameWithoutExtension(path), path))
            .OrderBy(c => c.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<ContextInfo>>(result);
    }

    public Task<ContextInfo> GetContextAsync(string name, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(name);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Contexto '{name}' no encontrado.");
        }

        return Task.FromResult(BuildContextInfo(name, filePath));
    }

    public async Task<string> GetContextContentAsync(string name, CancellationToken cancellationToken)
    {
        ValidateName(name);

        var filePath = GetFilePath(name);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Contexto '{name}' no encontrado.");
        }

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    public Task DeleteContextAsync(string name, CancellationToken cancellationToken)
    {
        ValidateName(name);
        var filePath = GetFilePath(name);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Contexto '{name}' no encontrado.");
        }

        File.Delete(filePath);
        _contextsCache.TryRemove(name, out _);
        return Task.CompletedTask;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        ValidateOptions();

        var contextName = request.ContextName;
        if (string.IsNullOrWhiteSpace(contextName))
            throw new InvalidOperationException("Debes especificar un contextName en la petición.");

        if (!_contextsCache.TryGetValue(contextName, out var contextMarkdown))
        {
            var filePath = GetFilePath(contextName);
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Contexto '{contextName}' no encontrado. Sube un archivo .md con ese nombre primero.");
            }

            contextMarkdown = await File.ReadAllTextAsync(filePath, cancellationToken);
            _contextsCache[contextName] = contextMarkdown;
        }

        var messages = new List<ChatRequestMessage>
        {
            new ChatRequestSystemMessage(BuildSystemInstruction()),
            new ChatRequestSystemMessage($"Game context (Markdown):\n{contextMarkdown}"),
            new ChatRequestSystemMessage(BuildHistoryInstruction(request.RecentCards)),
            new ChatRequestUserMessage(request.Prompt)
        };

        var raw = await CompleteTextAsync(messages, cancellationToken);
        return ChatResponseMapper.Map(raw);
    }

    private static string BuildSystemInstruction()
    {
        return """
               You are a narrative assistant for a swipe-based decision videogame.
               Always respond with valid JSON only, no markdown, no extra text.
               The root JSON must contain exactly: type, role, name, gender, situation, left_option, right_option, effects, theme.
               The "theme" field must be one of: technical, financial, HR, conflict, crisis, strategy, personal, absurd/unexpected.
               """;
    }

    private static string BuildHistoryInstruction(IReadOnlyList<RecentCard> recentCards)
    {
        if (recentCards.Count == 0)
        {
            return "This is the first card of the session. Generate a completely new profile.";
        }

        var lines = recentCards.Select(c =>
            string.Equals(c.Type, "EVENT", StringComparison.OrdinalIgnoreCase)
                ? $"- [EVENT] theme:{c.Theme ?? "unknown"}"
                : $"- {c.Name ?? "?"} ({c.Role ?? "?"}) gender:{c.Gender ?? "?"} theme:{c.Theme ?? "unknown"}");

        return $"""
                ANTI-REPETITION: The following cards have already been shown in this session.
                Use this list ONLY to enforce variety rules. Do NOT continue their narrative.
                Do NOT reuse names, avoid repeating consecutive roles, balance genders, rotate themes.
                Recent cards:
                {string.Join("\n", lines)}
                """;
    }

    private async Task<string> CompleteTextAsync(IReadOnlyList<ChatRequestMessage> messages, CancellationToken cancellationToken)
    {
        if (IsResponsesEndpoint(_options.Endpoint))
        {
            return await CompleteWithResponsesApiAsync(messages, cancellationToken);
        }

        var endpoint = NormalizeEndpoint(_options.Endpoint);
        var client = new ChatCompletionsClient(new Uri(endpoint), new AzureKeyCredential(_options.ApiKey));
        var options = new ChatCompletionsOptions(messages)
        {
            Model = _options.Model,
            Temperature = 0.6f,
            MaxTokens = 400
        };

        var completion = await client.CompleteAsync(options, cancellationToken);
        return completion.Value.Content;
    }

    private async Task<string> CompleteWithResponsesApiAsync(IReadOnlyList<ChatRequestMessage> messages, CancellationToken cancellationToken)
    {
        var url = _options.Endpoint.Trim();
        var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("api-key", _options.ApiKey);

        var payload = new { model = _options.Model, input = messages.Select(ToResponsesInput) };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure OpenAI Responses API devolvió {(int)response.StatusCode}: {body}");
        }

        return ExtractResponseText(body);
    }

    private static object ToResponsesInput(ChatRequestMessage message)
    {
        var role = message switch
        {
            ChatRequestSystemMessage => "system",
            ChatRequestAssistantMessage => "assistant",
            _ => "user"
        };

        var text = message switch
        {
            ChatRequestSystemMessage m => m.Content,
            ChatRequestAssistantMessage m => m.Content,
            ChatRequestUserMessage m => m.Content,
            _ => string.Empty
        };

        return new { role, content = text };
    }

    private static string ExtractResponseText(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;

                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }
        }

        return json;
    }

    private static bool IsResponsesEndpoint(string endpoint)
        => endpoint.Contains("/openai/responses", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint);
        var builder = new UriBuilder(uri) { Path = uri.AbsolutePath.TrimEnd('/'), Query = string.Empty };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private static string GetFilePath(string name) => Path.Combine(ContextsDirectory, $"{name}.md");

    private static ContextInfo BuildContextInfo(string name, string filePath)
    {
        var info = new FileInfo(filePath);
        return new ContextInfo
        {
            Name = name,
            Version = info.LastWriteTimeUtc.ToString("yyyyMMddHHmmss"),
            UploadedAt = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            SizeBytes = info.Length
        };
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(InvalidNameChars) >= 0)
        {
            throw new ArgumentException($"Nombre de contexto inválido: '{name}'.");
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("Falta AzureFoundry:Endpoint.");
        if (string.IsNullOrWhiteSpace(_options.Model))
            throw new InvalidOperationException("Falta AzureFoundry:Model.");
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Falta AzureFoundry:ApiKey.");
        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out _))
            throw new InvalidOperationException("AzureFoundry:Endpoint no es una URL válida.");
    }

    private void LoadAllContextsFromDisk()
    {
        foreach (var file in Directory.GetFiles(ContextsDirectory, "*.md"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            _contextsCache[name] = File.ReadAllText(file);
        }
    }
}
