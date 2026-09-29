using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NegotiationClient : MonoBehaviour
{
    public AiStudioConfig config;

    private const string ResultSchemaJson =
        "{\"type\":\"object\",\"properties\":{" +
        "\"npc_reply\":{\"type\":\"string\",\"description\":\"Ответ NPC игроку с учётом заданного тона\"}," +
        "\"hit_quality\":{\"type\":\"string\",\"enum\":[\"perfect\",\"partial\",\"miss\"],\"description\":\"Насколько реплика игрока попадает в уязвимость NPC\"}," +
        "\"sprite_reaction\":{\"type\":\"string\",\"enum\":[\"approval\",\"neutral\",\"disapproval\"]}" +
        "},\"required\":[\"npc_reply\",\"hit_quality\",\"sprite_reaction\"]}";

    public void Evaluate(string systemPrompt, string playerMessage, Action<NegotiationResult> onResult, Action<string> onError)
    {
        string input = systemPrompt + "\n\nРеплика игрока: " + playerMessage;
        string json = "{" +
            "\"model\":\"" + JsonEscape(config.ModelUri) + "\"," +
            "\"input\":\"" + JsonEscape(input) + "\"," +
            "\"temperature\":0.5," +
            "\"max_output_tokens\":1500," +
            "\"reasoning\":{\"effort\":\"none\"}," +
            "\"response_format\":{\"type\":\"json_schema\",\"json_schema\":{\"name\":\"negotiation_result\",\"schema\":" + ResultSchemaJson + "}}" +
            "}";

        StartCoroutine(PostJson(json, raw => HandleEvaluateResponse(raw, onResult, onError), onError));
    }

    public void GenerateText(string prompt, Action<string> onText, Action<string> onError)
    {
        string json = "{" +
            "\"model\":\"" + JsonEscape(config.ModelUri) + "\"," +
            "\"input\":\"" + JsonEscape(prompt) + "\"," +
            "\"temperature\":0.6," +
            "\"max_output_tokens\":6000," +
            "\"reasoning\":{\"effort\":\"none\"}" +
            "}";

        StartCoroutine(PostJson(json, raw => HandleTextResponse(raw, onText, onError), onError));
    }

    private void HandleEvaluateResponse(string raw, Action<NegotiationResult> onResult, Action<string> onError)
    {
        string apiError = ExtractApiError(raw);
        if (apiError != null) { onError?.Invoke(apiError); return; }

        string modelOutputText = ExtractOutputText(raw);
        if (modelOutputText == null)
        {
            onError?.Invoke("Не удалось извлечь текст ответа модели, сырой JSON:\n" + raw);
            return;
        }

        NegotiationResult result;
        try
        {
            string jsonPart = ExtractJsonObject(modelOutputText);
            result = JsonUtility.FromJson<NegotiationResult>(jsonPart);
        }
        catch (Exception e)
        {
            onError?.Invoke("Не удалось распарсить структурированный результат: " + e.Message + "\nТекст модели:\n" + modelOutputText);
            return;
        }

        if (result == null || string.IsNullOrEmpty(result.npc_reply))
        {
            onError?.Invoke("Пустой результат разбора. Текст модели:\n" + modelOutputText);
            return;
        }

        onResult?.Invoke(result);
    }

    private void HandleTextResponse(string raw, Action<string> onText, Action<string> onError)
    {
        string apiError = ExtractApiError(raw);
        if (apiError != null) { onError?.Invoke(apiError); return; }

        string modelOutputText = ExtractOutputText(raw);
        if (modelOutputText == null)
        {
            onError?.Invoke("Не удалось извлечь текст ответа модели, сырой JSON:\n" + raw);
            return;
        }

        onText?.Invoke(modelOutputText);
    }

    private IEnumerator PostJson(string json, Action<string> onRawResponse, Action<string> onError)
    {
        var req = new UnityWebRequest(config.endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Api-Key " + config.apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string body = req.downloadHandler.text;
            req.Dispose();
            onError?.Invoke($"HTTP {req.responseCode}: {req.error}\n{body}");
            yield break;
        }

        string raw = req.downloadHandler.text;
        req.Dispose();
        onRawResponse?.Invoke(raw);
    }

    private static string JsonEscape(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    [Serializable] private class OutputContentPart { public string text; }
    [Serializable] private class OutputItem { public OutputContentPart[] content; }
    [Serializable] private class ChatChoiceMessage { public string content; }
    [Serializable] private class ChatChoice { public ChatChoiceMessage message; }

    [Serializable] private class ApiErrorDto { public string code; public string message; }

    [Serializable]
    private class ResponsesResponseDto
    {
        public OutputItem[] output;
        public string output_text;
        public ChatChoice[] choices;
        public ApiErrorDto error;
    }

    private static string ExtractApiError(string raw)
    {
        ResponsesResponseDto resp;
        try { resp = JsonUtility.FromJson<ResponsesResponseDto>(raw); }
        catch { return null; }

        if (string.IsNullOrEmpty(resp?.error?.code)) return null;

        if (resp.error.code == "rate_limit_exceeded")
            return "Сервер временно перегружен запросами. Подождите несколько секунд и отправьте реплику ещё раз.";

        return "Ошибка API (" + resp.error.code + "): " + resp.error.message;
    }

    private static string ExtractOutputText(string raw)
    {
        ResponsesResponseDto resp;
        try { resp = JsonUtility.FromJson<ResponsesResponseDto>(raw); }
        catch { return null; }

        if (resp == null) return null;
        if (!string.IsNullOrEmpty(resp.output_text)) return resp.output_text;

        if (resp.output != null)
        {
            foreach (var item in resp.output)
            {
                if (item.content == null) continue;
                foreach (var part in item.content)
                    if (!string.IsNullOrEmpty(part.text)) return part.text;
            }
        }

        if (resp.choices != null && resp.choices.Length > 0 && resp.choices[0].message != null)
            return resp.choices[0].message.content;

        return null;
    }

    private static string ExtractJsonObject(string text)
    {
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
        return text;
    }
}
