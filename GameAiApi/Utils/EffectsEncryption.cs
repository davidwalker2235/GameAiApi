using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GameAiApi.Utils;

public sealed class PlainEffectsPayload
{
    public Dictionary<string, int> Left { get; set; } = [];
    public Dictionary<string, int> Right { get; set; } = [];
}

public sealed class EncryptedEffectsPayload
{
    public string Ciphertext { get; set; } = string.Empty;
    public string Iv { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
}

public static class EffectsEncryption
{
    public const string SharedSecret = "Ks3a+4Ld";
    public const int Pbkdf2Iterations = 100000;
    public const int KeySizeBytes = 32;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static EncryptedEffectsPayload Encrypt(PlainEffectsPayload effects, string situation)
    {
        if (effects is null)
            throw new ArgumentNullException(nameof(effects));

        if (string.IsNullOrWhiteSpace(situation))
            throw new InvalidOperationException("No se puede encriptar 'effects' sin 'situation'.");

        var effectsJson = JsonSerializer.Serialize(effects, SerializerOptions);
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var ivBytes = RandomNumberGenerator.GetBytes(16);
        var key = DeriveKey(situation, saltBytes);

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = ivBytes;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(effectsJson);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return new EncryptedEffectsPayload
        {
            Ciphertext = Convert.ToBase64String(cipherBytes),
            Iv = Convert.ToBase64String(ivBytes),
            Salt = Convert.ToBase64String(saltBytes)
        };
    }

    public static PlainEffectsPayload Decrypt(EncryptedEffectsPayload encryptedEffects, string situation)
    {
        if (encryptedEffects is null)
            throw new ArgumentNullException(nameof(encryptedEffects));

        if (string.IsNullOrWhiteSpace(situation))
            throw new InvalidOperationException("No se puede derivar clave sin 'situation'.");

        if (string.IsNullOrWhiteSpace(encryptedEffects.Ciphertext) ||
            string.IsNullOrWhiteSpace(encryptedEffects.Iv) ||
            string.IsNullOrWhiteSpace(encryptedEffects.Salt))
        {
            throw new InvalidOperationException("effects incompleto (ciphertext/iv/salt).");
        }

        var cipherBytes = Convert.FromBase64String(encryptedEffects.Ciphertext);
        var ivBytes = Convert.FromBase64String(encryptedEffects.Iv);
        var saltBytes = Convert.FromBase64String(encryptedEffects.Salt);

        if (ivBytes.Length != 16)
            throw new InvalidOperationException($"IV inválido. Longitud esperada: 16 bytes. Actual: {ivBytes.Length}");

        if (saltBytes.Length == 0)
            throw new InvalidOperationException("Salt inválido. No puede estar vacío.");

        var key = DeriveKey(situation, saltBytes);

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = ivBytes;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        var effectsJson = Encoding.UTF8.GetString(plainBytes);

        var decryptedEffects = JsonSerializer.Deserialize<PlainEffectsPayload>(effectsJson, SerializerOptions);
        if (decryptedEffects is null || decryptedEffects.Left is null || decryptedEffects.Right is null)
            throw new InvalidOperationException("Los effects desencriptados no contienen left/right válidos.");

        return decryptedEffects;
    }

    private static byte[] DeriveKey(string situation, byte[] saltBytes)
    {
        var keyMaterial = $"{situation}|{SharedSecret}";
        using var kdf = new Rfc2898DeriveBytes(keyMaterial, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(KeySizeBytes);
    }
}
