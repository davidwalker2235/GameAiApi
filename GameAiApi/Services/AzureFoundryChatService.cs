using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.AI.Inference;
using GameAiApi.Contracts;
using GameAiApi.Options;
using Microsoft.Extensions.Options;

namespace GameAiApi.Services;

public sealed class AzureFoundryChatService : IAiChatService
{
    private const string ContextFilePath = "App_Data/context.md";

    private readonly AzureFoundryOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private string? _contextMarkdown;
    private string? _contextVersion;

    public AzureFoundryChatService(IOptions<AzureFoundryOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        LoadContextFromDisk();
    }

    public async Task<ContextStatusResponse> UploadContextAsync(Stream markdownStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(markdownStream, Encoding.UTF8, leaveOpen: true);
        var markdown = await reader.ReadToEndAsync(cancellationToken);

        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            _contextMarkdown = markdown;
            _contextVersion = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");

            var directory = Path.GetDirectoryName(ContextFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(ContextFilePath, markdown, cancellationToken);

            return new ContextStatusResponse
            {
                HasContext = !string.IsNullOrWhiteSpace(_contextMarkdown),
                Version = _contextVersion
            };
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<ContextStatusResponse> GetContextStatusAsync(CancellationToken cancellationToken)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            return new ContextStatusResponse
            {
                HasContext = !string.IsNullOrWhiteSpace(_contextMarkdown),
                Version = _contextVersion
            };
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        ValidateOptions();

        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(_contextMarkdown))
            {
                throw new InvalidOperationException("No hay contexto Markdown cargado. Sube un archivo .md primero.");
            }

            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(BuildSystemInstruction()),
                new ChatRequestSystemMessage($"Contexto del juego en Markdown:\n{_contextMarkdown}"),
                new ChatRequestUserMessage($"SessionId: {request.SessionId}\nPrompt: {request.Prompt}")
            };

            var raw = await CompleteTextAsync(messages, cancellationToken);

            JsonElement? structured = null;
            try
            {
                structured = JsonDocument.Parse(raw).RootElement.Clone();
            }
            catch (JsonException)
            {
            }

            return new ChatResponse
            {
                Raw = raw,
                Structured = structured
            };
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private static string BuildSystemInstruction()
    {
        return """
               Eres un asistente narrativo para videojuegos.
               Responde SIEMPRE y SOLO con JSON válido, sin markdown ni texto adicional.
               Usa exactamente esta estructura:
               {
                 "npcText": "string",
                 "emotion": "neutral|happy|angry|fear|sad",
                 "actions": ["string"],
                 "safety": {
                   "violent": false,
                   "sexual": false
                 }
               }
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

        var payload = new
        {
            model = _options.Model,
            input = messages.Select(ToResponsesInput)
        };

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

        return new
        {
            role,
            content = text
        };
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
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

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
    {
        return endpoint.Contains("/openai/responses", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint);
        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.TrimEnd('/'),
            Query = string.Empty
        };

        return builder.Uri.ToString().TrimEnd('/');
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("Falta AzureFoundry:Endpoint.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("Falta AzureFoundry:Model.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Falta AzureFoundry:ApiKey.");
        }

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("AzureFoundry:Endpoint no es una URL válida.");
        }
    }

    private void LoadContextFromDisk()
    {
        if (!File.Exists(ContextFilePath))
        {
            return;
        }

        _contextMarkdown = File.ReadAllText(ContextFilePath);
        _contextVersion = File.GetLastWriteTimeUtc(ContextFilePath).ToString("yyyyMMddHHmmss");
    }
}
