using UnityEngine;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour
{
    public AiStudioChatClient client;
    public Text logText;
    public InputField inputField;
    public Button sendButton;
    public ScrollRect scrollRect;

    private void Awake()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        inputField.onSubmit.AddListener(_ => OnSendClicked());
    }

    private void OnSendClicked()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        AppendLine("Вы: " + text);
        inputField.text = string.Empty;
        inputField.ActivateInputField();
        sendButton.interactable = false;

        client.SendUserMessage(text, OnReply, OnError);
    }

    private void OnReply(string reply)
    {
        sendButton.interactable = true;
        AppendLine("Бот: " + reply);
    }

    private void OnError(string error)
    {
        sendButton.interactable = true;
        AppendLine("[Ошибка] " + error);
        Debug.LogError(error);
    }

    private void AppendLine(string line)
    {
        logText.text += (string.IsNullOrEmpty(logText.text) ? "" : "\n\n") + line;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
