using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NegotiationUIController : MonoBehaviour
{
    public NegotiationSession session;
    public Text speakerNameText;
    public Text logText;
    public Text thinkingText;
    public InputField inputField;
    public Button sendButton;
    public ScrollRect scrollRect;
    public Image playerHpFillImage;
    public Image agentHpFillImage;
    public Text agentNameText;
    public RecapController recap;

    [Header("Visual novel art layer (optional)")]
    public GameObject artLayerRoot;
    public Image flatBackgroundImage;
    public Image sceneBackgroundImage;
    public Image playerPortraitImage;
    public Image npcPortraitImage;

    [Tooltip("NPC portrait art faces left by default; mirrored so it faces the player on the right.")]
    public float npcPortraitFlipX = -1f;

    [Tooltip("Fraction of screen width kept as a gap between each portrait and its screen edge.")]
    public float portraitEdgeMargin = 0.02f;
    [Tooltip("Portrait height as a fraction of screen height.")]
    public float portraitMaxHeightFraction = 0.72f;
    [Tooltip("Fraction of screen height where portraits' feet sit.")]
    public float portraitBottomAnchor = 0.10f;
    [Tooltip("Emergency-only cap on portrait width as a fraction of screen width, for an unusually " +
        "wide pose on an unusually narrow screen. Normally UNUSED: the box width is derived from " +
        "the sprite's own real aspect ratio (see FitPortraitToFrame), so preserveAspect never has " +
        "to shrink anything and both portraits render at the same base height regardless of pose. " +
        "This only kicks in - shrinking height along with width to keep the pose undistorted - if " +
        "that natural width would run off the far edge of the screen. Overlap between the two " +
        "portraits is fine and expected.")]
    public float portraitMaxWidthFraction = 0.95f;

    private static readonly Color ActiveTint = Color.white;
    private static readonly Color InactiveTint = new Color(0.5f, 0.5f, 0.5f, 1f);
    private const float ActiveScale = 1.08f;
    private const float InactiveScale = 0.92f;
    private const float PortraitTransitionSeconds = 0.25f;
    private const float FinalLineReadDelaySeconds = 6f;

    private Coroutine _playerPortraitAnim;
    private Coroutine _npcPortraitAnim;

    private bool _battleOver;
    private bool _waitingForResponse;

    private void Awake()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        inputField.onSubmit.AddListener(_ => OnSendClicked());
    }

    public void BeginBattle()
    {
        _battleOver = false;
        _waitingForResponse = false;
        inputField.text = string.Empty;
        inputField.interactable = true;
        sendButton.interactable = true;
        if (thinkingText != null) thinkingText.gameObject.SetActive(false);

        session.BeginSession();
        if (agentNameText != null) agentNameText.text = session.scenario.npcName;
        UpdateHpBars();
        SetupArtLayer();
        LayoutInputRow();
        ShowLine(session.scenario.npcName, session.scenario.openingLine);
    }

    private void LayoutInputRow()
    {
        var sendRect = (RectTransform)sendButton.transform;
        var inputRect = (RectTransform)inputField.transform;
        var parent = inputRect.parent as RectTransform;
        if (parent == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(sendRect);

        float parentWidth = parent.rect.width;
        float gapPx = parentWidth * 0.045f;
        float sendRightX = sendRect.anchorMin.x * parentWidth;
        float sendLeftX = sendRightX - sendRect.rect.width;

        float inputRightFraction = (sendLeftX - gapPx) / parentWidth;
        inputRect.anchorMax = new Vector2(inputRightFraction, inputRect.anchorMax.y);
    }

    private void SetupArtLayer()
    {
        var scenario = session.scenario;
        bool hasArt = scenario.background != null;

        if (artLayerRoot != null) artLayerRoot.SetActive(hasArt);
        if (flatBackgroundImage != null)
            flatBackgroundImage.color = hasArt ? new Color(1f, 1f, 1f, 0f) : Color.white;

        if (!hasArt) return;

        if (sceneBackgroundImage != null)
        {
            sceneBackgroundImage.sprite = scenario.background;
            FitBackgroundCover(sceneBackgroundImage);
        }
        SetPortraitSprite(playerPortraitImage, scenario.GetPlayerListeningPortrait(), pinnedRight: true);
        SetPortraitSprite(npcPortraitImage, scenario.GetNpcSpeakingPortrait(session.tone, SpriteReaction.Neutral), pinnedRight: false);

        SnapSpeaker(playerSpeaking: false);
    }

    private void OnSendClicked()
    {
        if (_battleOver || _waitingForResponse) return;

        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        ShowLine("Вы", text);
        inputField.text = string.Empty;
        _waitingForResponse = true;
        sendButton.interactable = false;
        inputField.interactable = false;

        SetPortraitSprite(npcPortraitImage, session.scenario.GetNpcSpeakingPortrait(session.tone, SpriteReaction.Neutral), pinnedRight: false);
        SetPortraitSprite(playerPortraitImage, session.scenario.playerPortrait, pinnedRight: true, extraOffsetXFraction: session.scenario.playerSpeakingPortraitOffsetX);
        SetSpeaker(playerSpeaking: true);

        if (thinkingText != null)
        {
            thinkingText.text = "думает...";
            thinkingText.gameObject.SetActive(true);
        }

        session.TakeTurn(text, OnOutcome, OnError);
    }

    private void OnOutcome(TurnOutcome outcome)
    {
        _waitingForResponse = false;
        if (thinkingText != null) thinkingText.gameObject.SetActive(false);

        bool battleEnded = outcome.AgentDefeated || outcome.PlayerDefeated;
        bool victory = outcome.AgentDefeated;

        SetPortraitSprite(npcPortraitImage, session.scenario.GetNpcSpeakingPortrait(session.tone, outcome.SpriteReaction), pinnedRight: false);

        var playerSprite = battleEnded
            ? (victory ? session.scenario.playerVictoryPortrait : session.scenario.playerDefeatPortrait)
            : session.scenario.GetPlayerListeningPortrait();
        SetPortraitSprite(playerPortraitImage, playerSprite, pinnedRight: true);

        SetSpeaker(playerSpeaking: false);

        ShowLine(session.scenario.npcName, outcome.NpcReply);
        UpdateHpBars();

        if (!battleEnded)
        {
            sendButton.interactable = true;
            inputField.interactable = true;
            inputField.ActivateInputField();
            return;
        }

        _battleOver = true;
        if (victory && !string.IsNullOrEmpty(session.scenario.unlocksScenarioId))
            ScenarioProgress.Unlock(session.scenario.unlocksScenarioId);

        recap.BeginReview(victory);
        StartCoroutine(ShowRecapAfterDelay(victory));
    }

    private IEnumerator ShowRecapAfterDelay(bool victory)
    {
        yield return new WaitForSeconds(FinalLineReadDelaySeconds);
        recap.Show(victory);
    }

    private void OnError(string error)
    {
        _waitingForResponse = false;
        if (thinkingText != null) thinkingText.gameObject.SetActive(false);
        sendButton.interactable = true;
        inputField.interactable = true;
        inputField.ActivateInputField();
        ShowLine("Ошибка", error);
        Debug.LogError(error);
    }

    private static void FitBackgroundCover(Image image)
    {
        if (image == null || image.sprite == null) return;

        var rect = image.rectTransform;
        var parent = rect.parent as RectTransform;
        if (parent == null) return;

        image.preserveAspect = false;

        float spriteAspect = image.sprite.rect.width / image.sprite.rect.height;
        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        float parentAspect = parentWidth / parentHeight;

        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        if (spriteAspect >= parentAspect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(parentHeight * spriteAspect, 0f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(0f, parentWidth / spriteAspect);
        }
    }

    private void SetPortraitSprite(Image image, Sprite sprite, bool pinnedRight, float extraOffsetXFraction = 0f)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        FitPortraitToFrame(image, pinnedRight, extraOffsetXFraction);
    }

    private void FitPortraitToFrame(Image image, bool pinnedRight, float extraOffsetXFraction = 0f)
    {
        if (image == null || image.sprite == null) return;

        var rect = image.rectTransform;
        var parent = rect.parent as RectTransform;
        if (parent == null) return;

        image.preserveAspect = true;

        float edgeAnchor = pinnedRight ? 1f : 0f;
        rect.anchorMin = new Vector2(edgeAnchor, portraitBottomAnchor);
        rect.anchorMax = new Vector2(edgeAnchor, portraitBottomAnchor);
        rect.pivot = new Vector2(0.5f, 0f);

        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;

        float boxHeight = parentHeight * portraitMaxHeightFraction;
        float spriteAspect = image.sprite.rect.width / image.sprite.rect.height;
        float boxWidth = boxHeight * spriteAspect;

        float margin = parentWidth * portraitEdgeMargin;
        float maxWidth = parentWidth * portraitMaxWidthFraction - margin;
        if (boxWidth > maxWidth)
        {
            boxWidth = maxWidth;
            boxHeight = boxWidth / spriteAspect;
        }

        rect.sizeDelta = new Vector2(boxWidth, boxHeight);

        float centerOffset = margin + boxWidth / 2f;
        float x = pinnedRight ? -centerOffset : centerOffset;
        x += extraOffsetXFraction * parentWidth;
        rect.anchoredPosition = new Vector2(x, 0f);
    }

    private void UpdateHpBars()
    {
        float maxHp = DifficultyRules.StartingHp(session.difficulty);
        if (playerHpFillImage != null) playerHpFillImage.fillAmount = Mathf.Clamp01(session.PlayerHp / maxHp);
        if (agentHpFillImage != null) agentHpFillImage.fillAmount = Mathf.Clamp01(session.AgentHp / maxHp);
    }

    private void ShowLine(string speaker, string line)
    {
        if (speakerNameText != null) speakerNameText.text = speaker;
        logText.text = line;
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void SnapSpeaker(bool playerSpeaking)
    {
        SnapPortrait(playerPortraitImage, playerSpeaking, 1f);
        SnapPortrait(npcPortraitImage, !playerSpeaking, npcPortraitFlipX);
        BringSpeakerToFront(playerSpeaking);
    }

    private void SetSpeaker(bool playerSpeaking)
    {
        AnimatePortrait(playerPortraitImage, playerSpeaking, 1f, ref _playerPortraitAnim);
        AnimatePortrait(npcPortraitImage, !playerSpeaking, npcPortraitFlipX, ref _npcPortraitAnim);
        BringSpeakerToFront(playerSpeaking);
    }

    private void BringSpeakerToFront(bool playerSpeaking)
    {
        if (playerPortraitImage == null || npcPortraitImage == null) return;
        var front = playerSpeaking ? playerPortraitImage.transform : npcPortraitImage.transform;
        front.SetAsLastSibling();
    }

    private static void SnapPortrait(Image image, bool active, float flipX)
    {
        if (image == null) return;
        float scale = active ? ActiveScale : InactiveScale;
        image.rectTransform.localScale = new Vector3(scale * flipX, scale, scale);
        image.color = active ? ActiveTint : InactiveTint;
    }

    private void AnimatePortrait(Image image, bool active, float flipX, ref Coroutine handle)
    {
        if (image == null) return;
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(AnimatePortraitRoutine(image, active, flipX));
    }

    private static IEnumerator AnimatePortraitRoutine(Image image, bool active, float flipX)
    {
        var rect = image.rectTransform;
        Vector3 startScale = rect.localScale;
        float targetMagnitude = active ? ActiveScale : InactiveScale;
        Vector3 targetScale = new Vector3(targetMagnitude * flipX, targetMagnitude, targetMagnitude);
        Color startColor = image.color;
        Color targetColor = active ? ActiveTint : InactiveTint;

        float t = 0f;
        while (t < PortraitTransitionSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / PortraitTransitionSeconds);
            rect.localScale = Vector3.Lerp(startScale, targetScale, k);
            image.color = Color.Lerp(startColor, targetColor, k);
            yield return null;
        }

        rect.localScale = targetScale;
        image.color = targetColor;
    }
}
