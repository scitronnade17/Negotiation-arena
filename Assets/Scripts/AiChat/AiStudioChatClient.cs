using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable] internal class ResponsesRequestDto
{
    public string model;
    public string input;
    public float temperature = 0.6f;
    public int max_output_tokens = 800;
}

[Serializable] internal class OutputContentPart { public string text; }
[Serializable] internal class OutputItem { public OutputContentPart[] content; }
[Serializable] internal class ChatChoiceMessage { public string content; }
[Serializable] internal class ChatChoice { public ChatChoiceMessage message; }

[Serializable]
internal class ResponsesResponseDto
{
    public OutputItem[] output;
    public string output_text;
    public ChatChoice[] choices;
}

public class AiStudioChatClient : MonoBehaviour
{
    public AiStudioConfig config;

    private readonly List<string> _transcript = new List<string>();

    public void SendUserMessage(string userText, Action<string> onReply, Action<string> onError)
    {
        if (config == null || string.IsNullOrEmpty(config.apiKey) || string.IsNullOrEmpty(config.folderId))
        {
            onError?.Invoke("Не задан AiStudioConfig (apiKey/folderId). Заполните поля в Inspector.");
            return;
        }

        _transcript.Add("User: " + userText);
        StartCoroutine(SendRequest(onReply, onError));
    }

    private IEnumerator SendRequest(Action<string> onReply, Action<string> onError)
    {
        var body = new ResponsesRequestDto
        {
            model = config.ModelUri,
            input = BuildInput(),
            temperature = 0.6f,
            max_output_tokens = 800
        };
        string json = JsonUtility.ToJson(body);

        var req = new UnityWebRequest(config.endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Api-Key " + config.apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"HTTP {req.responseCode}: {req.error}\n{req.downloadHandler.text}");
            req.Dispose();
            yield break;
        }

        string raw = req.downloadHandler.text;
        req.Dispose();

        ResponsesResponseDto resp;
        try
        {
            resp = JsonUtility.FromJson<ResponsesResponseDto>(raw);
        }
        catch (Exception e)
        {
            onError?.Invoke("Не удалось распарсить ответ: " + e.Message + "\n" + raw);
            yield break;
        }

        string reply = ExtractReplyText(resp);
        if (reply == null)
        {
            onError?.Invoke("Неизвестный формат ответа, сырой JSON:\n" + raw);
            yield break;
        }

        _transcript.Add("Assistant: " + reply);
        onReply?.Invoke(reply);
    }

    private string BuildInput()
    {
        var sb = new StringBuilder();
        sb.AppendLine(config.systemPrompt);
        foreach (var line in _transcript)
            sb.AppendLine(line);
        return sb.ToString();
    }

    private static string ExtractReplyText(ResponsesResponseDto resp)
    {
        if (!string.IsNullOrEmpty(resp.output_text)) return resp.output_text;

        if (resp.output != null)
        {
            foreach (var item in resp.output)
            {
                if (item.content == null) continue;
                foreach (var part in item.content)
                {
                    if (!string.IsNullOrEmpty(part.text)) return part.text;
                }
            }
        }

        if (resp.choices != null && resp.choices.Length > 0 && resp.choices[0].message != null)
            return resp.choices[0].message.content;

        return null;
    }
}
