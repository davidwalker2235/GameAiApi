using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Networking;

[System.Serializable]
public class CharacterEffect
{
    public int money = 0;
    public int reputation = 0;
    public int people = 0;
    public int energy = 0;
}

[System.Serializable]
public class ScenarioEffects
{
    public CharacterEffect left;
    public CharacterEffect right;
    public string ciphertext;
    public string iv;
    public string salt;
}

[System.Serializable]
public class AIScenario
{
    public string type;
    public string theme;
    public string name;
    public string role;
    public string gender;
    public string situation;
    public string left_option;
    public string right_option;
    public ScenarioEffects effects;
}

[System.Serializable]
public class GenerationResponse
{
    public bool success;
    public List<string> errors = new List<string>();
    public AIScenario data;
}

[System.Serializable]
public class RecentCard
{
    public string name;
    public string role;
    public string type;
    public string gender;
    public string theme;
}

[System.Serializable]
public class AIGenerationRequest
{
    public string prompt;
    public string contextName;
    public List<RecentCard> recentCards;
}

public class AIScenarioGenerator : MonoBehaviour
{
    private const string API_URL = "https://softwareengineering-gzbrg3f6evdpb5ff.canadacentral-01.azurewebsites.net/api/ai/chat";
    private const int MAX_HISTORY_SIZE = 10;
    private const string SHARED_SECRET = "Ks3a+4Ld";
    private const int PBKDF2_ITERATIONS = 100000;
    private const int KEY_SIZE_BYTES = 32;

    private List<AIScenario> scenarioHistory = new List<AIScenario>();
    private bool isGenerating = false;

    public static AIScenarioGenerator Instance;

    public Action<AIScenario> onAIScenarioGenerated;
    public Action<List<string>> onAIGenerationError;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void GenerateScenario(int numScenarios = 1)
    {
        if (isGenerating)
        {
            Debug.LogWarning("Ya hay una generación en proceso");
            return;
        }

        StartCoroutine(GenerateScenarioCoroutine(numScenarios));
    }

    private IEnumerator GenerateScenarioCoroutine(int numScenarios)
    {
        isGenerating = true;
        while (numScenarios > 0)
        {
            --numScenarios;
            // Preparar el input con clases serializables
            List<RecentCard> cleanHistory = CleanHistoryForInput(scenarioHistory);
            
            var inputData = new AIGenerationRequest
            {
                prompt = "start",
                contextName = "es_context",
                recentCards = cleanHistory
            };

            string jsonInput = JsonUtility.ToJson(inputData);

            // Hacer la request
            using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonInput);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                isGenerating = false;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onAIGenerationError?.Invoke(new List<string> { $"HTTP Error: {request.error}" });
                    yield break;
                }

                try
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"Response: {responseText}");

                    AIScenario data = JsonUtility.FromJson<AIScenario>(responseText);

                    List<string> decryptionErrors = TryDecryptEffectsIfNeeded(data);
                    if (decryptionErrors.Count > 0)
                    {
                        onAIGenerationError?.Invoke(decryptionErrors);
                        yield break;
                    }

                    // Validar
                    List<string> errors = ValidateResponse(data);

                    if (errors.Count > 0)
                    {
                        Debug.LogError($"Errores de validación: {string.Join(", ", errors)}");
                        onAIGenerationError?.Invoke(errors);
                        yield break;
                    }

                    // Guardar en historial
                    scenarioHistory.Add(data);
                    if (scenarioHistory.Count > MAX_HISTORY_SIZE)
                    {
                        scenarioHistory.RemoveAt(0);
                    }

                    onAIScenarioGenerated?.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception: {e.Message}\n{e.StackTrace}");
                    onAIGenerationError?.Invoke(new List<string> { $"Exception: {e.Message}" });
                }
            }
        }
    }

    private List<string> ValidateResponse(AIScenario data)
    {
        List<string> errors = new List<string>();

        if (data == null)
        {
            errors.Add("Respuesta JSON nula");
            return errors;
        }

        // Campos básicos - más flexible
        if (string.IsNullOrEmpty(data.situation))
            errors.Add("Falta campo: situation");
        if (string.IsNullOrEmpty(data.left_option))
            errors.Add("Falta campo: left_option");
        if (string.IsNullOrEmpty(data.right_option))
            errors.Add("Falta campo: right_option");

        // Efectos - obligatorio
        if (data.effects == null)
        {
            errors.Add("Faltan effects");
        }
        else
        {
            if (data.effects.left == null)
                errors.Add("Faltan efectos para left");
            if (data.effects.right == null)
                errors.Add("Faltan efectos para right");
        }

        return errors;
    }

    private List<string> TryDecryptEffectsIfNeeded(AIScenario data)
    {
        List<string> errors = new List<string>();

        if (data == null)
        {
            return errors;
        }

        if (data.effects == null)
        {
            errors.Add("Falta effects");
            return errors;
        }

        if (data.effects.left != null || data.effects.right != null)
        {
            errors.Add("No se permite effects en claro. Debe venir effects encriptado");
            return errors;
        }

        if (string.IsNullOrEmpty(data.situation))
        {
            errors.Add("No se puede derivar clave sin situation");
            return errors;
        }

        if (string.IsNullOrEmpty(data.effects.ciphertext) ||
            string.IsNullOrEmpty(data.effects.iv) ||
            string.IsNullOrEmpty(data.effects.salt))
        {
            errors.Add("effects incompleto (ciphertext/iv/salt)");
            return errors;
        }

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(data.effects.ciphertext);
            byte[] ivBytes = Convert.FromBase64String(data.effects.iv);
            byte[] saltBytes = Convert.FromBase64String(data.effects.salt);

            string keyMaterial = $"{data.situation}|{SHARED_SECRET}";
            using var kdf = new Rfc2898DeriveBytes(keyMaterial, saltBytes, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);
            byte[] key = kdf.GetBytes(KEY_SIZE_BYTES);

            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = ivBytes;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            string effectsJson = Encoding.UTF8.GetString(plainBytes);

            ScenarioEffects decryptedEffects = JsonUtility.FromJson<ScenarioEffects>(effectsJson);
            if (decryptedEffects == null)
            {
                errors.Add("No se pudieron parsear los effects desencriptados");
                return errors;
            }

            data.effects = decryptedEffects;
        }
        catch (Exception ex)
        {
            errors.Add($"Error desencriptando effects: {ex.Message}");
        }

        return errors;
    }

    private List<RecentCard> CleanHistoryForInput(List<AIScenario> history)
    {
        List<RecentCard> cleanedHistory = new List<RecentCard>();

        int startIndex = Mathf.Max(0, history.Count - MAX_HISTORY_SIZE);
        for (int i = startIndex; i < history.Count; i++)
        {
            var scenario = history[i];
            var cleanedScenario = new RecentCard
            {
                name = scenario.name,
                role = scenario.role,
                type = scenario.type,
                gender = scenario.gender,
                theme = scenario.theme
                // NO incluir "situation"
            };
            cleanedHistory.Add(cleanedScenario);
        }

        return cleanedHistory;
    }
}

