using UnityEngine;

[CreateAssetMenu(fileName = "AiStudioConfig", menuName = "Negotiation Arena/AI Studio Config")]
public class AiStudioConfig : ScriptableObject
{
    [Tooltip("Yandex AI Studio API key (AI Studio console -> API keys). Keep out of version control.")]
    public string apiKey;

    [Tooltip("Yandex Cloud folder ID, shown at the top of the AI Studio console.")]
    public string folderId;

    [Tooltip("Model id from the AI Studio catalog.")]
    public string modelId = "deepseek-v4-flash";

    [Tooltip("Yandex AI Studio Responses API endpoint.")]
    public string endpoint = "https://ai.api.cloud.yandex.net/v1/responses";

    [TextArea(2, 5)]
    public string systemPrompt = "Ты дружелюбный тестовый ассистент. Отвечай кратко, на русском языке.";

    public string ModelUri => $"gpt://{folderId}/{modelId}";
}
