using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RecapController : MonoBehaviour
{
    public GameObject recapRoot;
    public GameObject battleRoot;
    public GameObject selectRoot;
    public NegotiationSession session;
    public NegotiationUIController battleUI;

    [Tooltip("Lives on an always-active object, not this screen - BeginReview() runs while " +
        "the battle screen is still showing (recapRoot inactive), and a coroutine can't start " +
        "on an inactive GameObject.")]
    public NegotiationClient reviewClient;

    public Text titleText;
    public Text bodyText;
    public Image headerBandImage;
    public Button playAgainButton;
    public Button backToSelectButton;

    private static readonly Color VictoryHeaderColor = new Color(0.85f, 0.96f, 0.87f);
    private static readonly Color DefeatHeaderColor = new Color(0.98f, 0.85f, 0.85f);

    private bool _reviewReady;
    private string _reviewText;

    private void Awake()
    {
        playAgainButton.onClick.AddListener(OnPlayAgain);
        backToSelectButton.onClick.AddListener(OnBackToSelect);
    }

    public void BeginReview(bool victory)
    {
        _reviewReady = false;
        _reviewText = null;

        if (session.History.Count == 0)
        {
            _reviewReady = true;
            _reviewText = "Раунд закончился без единой реплики — разбирать нечего.";
            return;
        }

        reviewClient.GenerateText(BuildReviewPrompt(victory), OnReviewReady, OnReviewError);
    }

    public void Show(bool victory)
    {
        battleRoot.SetActive(false);
        recapRoot.SetActive(true);

        titleText.text = victory ? "Победа!" : "Поражение";
        if (headerBandImage != null) headerBandImage.color = victory ? VictoryHeaderColor : DefeatHeaderColor;
        bodyText.text = _reviewReady ? _reviewText : "Провожу анализ диалога...";
    }

    private string BuildReviewPrompt(bool victory)
    {
        var scenario = session.scenario;
        var sb = new StringBuilder();

        sb.Append("Ты — коуч по переговорам. Разбери прошедший диалог игрока с персонажем по имени ")
            .Append(scenario.npcName).Append(".\n");
        sb.Append("Сценарий: ").Append(scenario.displayName).Append('\n');
        sb.Append("Уязвимость ").Append(scenario.npcName).Append(", в которую нужно было бить: ")
            .Append(scenario.vulnerability).Append('\n');
        sb.Append("Итог раунда: ").Append(victory ? "игрок победил" : "игрок проиграл").Append(".\n\n");
        sb.Append("Полная история переговоров (Оценка: perfect/partial/miss):\n");

        foreach (var turn in session.History)
        {
            sb.Append("Игрок: ").Append(turn.PlayerMessage).Append('\n');
            sb.Append("Оценка: ").Append(turn.Quality).Append('\n');
            sb.Append(scenario.npcName).Append(": ").Append(turn.NpcReply).Append("\n\n");
        }

        sb.Append("Напиши короткий разбор на русском языке СТРОГО в виде 3-4 пунктов списка, каждый пункт — " +
            "одно короткое предложение (не длиннее 12-14 слов), начинается с символа \"• \". Один пункт про то, " +
            "что игрок сделал хорошо (со ссылкой на конкретную реплику), один-два — какие конкретные реплики " +
            "были слабыми и почему именно, один — что попробовать в следующий раз, чтобы точнее бить в уязвимость ")
            .Append(scenario.npcName).Append(". ")
            .Append("Обращайся к игроку на \"ты\", а к персонажу — по имени ").Append(scenario.npcName)
            .Append(", НИКОГДА не называй его словом \"NPC\" или \"персонаж\" — только по имени, как в живом разговоре. " +
            "Без общих фраз вроде \"продолжай в том же духе\" — только конкретика по этому диалогу. " +
            "Никакого markdown кроме самого символа \"• \" в начале строки: без **, без #, без заголовков. " +
            "Это должно быть коротко и помещаться на маленький экран, не растекайся мыслью.");

        return sb.ToString();
    }

    private void OnReviewReady(string text)
    {
        _reviewReady = true;
        _reviewText = text.Trim();
        if (recapRoot.activeSelf) bodyText.text = _reviewText;
    }

    private void OnReviewError(string error)
    {
        _reviewReady = true;
        _reviewText = "Не удалось сгенерировать разбор диалога.\n" + error;
        if (recapRoot.activeSelf) bodyText.text = _reviewText;
        Debug.LogError(error);
    }

    private void OnPlayAgain()
    {
        recapRoot.SetActive(false);
        battleRoot.SetActive(true);
        battleUI.BeginBattle();
    }

    private void OnBackToSelect()
    {
        recapRoot.SetActive(false);
        selectRoot.SetActive(true);
    }
}
