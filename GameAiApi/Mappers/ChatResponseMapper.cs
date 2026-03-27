using System.Text.Json;
using GameAiApi.Contracts;
using GameAiApi.Domain;

namespace GameAiApi.Mappers;

public static class ChatResponseMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ChatResponse Map(string raw)
    {
        ChatResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<ChatResponse>(raw, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("La IA no devolvió un JSON válido.", ex);
        }

        if (response is null)
        {
            throw new InvalidOperationException("La IA devolvió una respuesta vacía.");
        }

        if (!AiResponseTypeExtensions.TryParseType(response.Type, out var parsedType))
        {
            throw new InvalidOperationException($"Valor inválido para 'type'. Valores permitidos: {string.Join(", ", AiResponseTypeCatalog.Types)}.");
        }

        if (!AiResponseTypeExtensions.TryParseRole(response.Role, out var parsedRole))
        {
            throw new InvalidOperationException($"Valor inválido para 'role'. Valores permitidos: {string.Join(", ", AiResponseTypeCatalog.Roles)}.");
        }

        response.Type = parsedType.ToApiValue();
        response.Role = parsedRole.ToApiValue();
        response.Effects ??= new EffectsResponse();
        response.Effects.Left ??= [];
        response.Effects.Right ??= [];
        EnsureDefaultEffectValues(response.Effects.Left);
        EnsureDefaultEffectValues(response.Effects.Right);

        return response;
    }

    private static void EnsureDefaultEffectValues(Dictionary<string, int> values)
    {
        EnsureKey(values, "money");
        EnsureKey(values, "reputation");
        EnsureKey(values, "people");
        EnsureKey(values, "energy");
    }

    private static void EnsureKey(Dictionary<string, int> values, string key)
    {
        if (!values.ContainsKey(key))
        {
            values[key] = 0;
        }
    }
}
