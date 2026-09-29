using UnityEngine;
using UnityEngine.UI;

public class ScenarioSelectController : MonoBehaviour
{
    public GameObject selectPanelRoot;
    public GameObject standardMenuPanelRoot;
    public GameObject battleRoot;
    public NegotiationSession session;
    public NegotiationUIController battleUI;

    public ScenarioDefinition hrScenario;
    public ScenarioDefinition partnersScenario;
    public Button hrButton;
    public Button partnersButton;

    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    public Button aggressiveButton;
    public Button friendlyButton;
    public Button neutralButton;
    public Button sarcasticButton;

    public Button randomButton;
    public Button backButton;

    public Text statusText;
    public Button playButton;

    public ScenarioDefinition selectedScenario;
    public Difficulty selectedDifficulty;
    public Tone selectedTone;

    private void Awake()
    {
        hrButton.onClick.AddListener(() => SelectScenario(hrScenario));
        partnersButton.onClick.AddListener(() => SelectScenario(partnersScenario));

        easyButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Easy));
        mediumButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Medium));
        hardButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Hard));

        aggressiveButton.onClick.AddListener(() => SelectTone(Tone.Aggressive));
        friendlyButton.onClick.AddListener(() => SelectTone(Tone.Friendly));
        neutralButton.onClick.AddListener(() => SelectTone(Tone.Neutral));
        sarcasticButton.onClick.AddListener(() => SelectTone(Tone.Sarcastic));

        randomButton.onClick.AddListener(Randomize);
        backButton.onClick.AddListener(OnBackClicked);
        playButton.onClick.AddListener(OnPlayClicked);

        if (selectedScenario == null)
            SelectScenario(hrScenario);
        else
            RefreshStatus();
    }

    private void OnEnable()
    {
        RefreshLockStates();
    }

    private void RefreshLockStates()
    {
        bool partnersUnlocked = ScenarioProgress.IsUnlocked(partnersScenario.scenarioId, false);
        partnersButton.interactable = partnersUnlocked;
        partnersButton.GetComponent<Image>().color = partnersUnlocked ? Color.white : Color.gray;

        var label = partnersButton.GetComponentInChildren<Text>();
        label.text = partnersUnlocked
            ? partnersScenario.displayName
            : partnersScenario.displayName + " (заблокировано)";

        if (selectedScenario == partnersScenario && !partnersUnlocked)
            SelectScenario(hrScenario);
    }

    public void SelectScenario(ScenarioDefinition scenario)
    {
        selectedScenario = scenario;
        selectedDifficulty = scenario.defaultDifficulty;
        selectedTone = scenario.defaultTone;
        RefreshStatus();
    }

    public void SelectDifficulty(Difficulty difficulty)
    {
        selectedDifficulty = difficulty;
        RefreshStatus();
    }

    public void SelectTone(Tone tone)
    {
        selectedTone = tone;
        RefreshStatus();
    }

    private void Randomize()
    {
        bool partnersUnlocked = ScenarioProgress.IsUnlocked(partnersScenario.scenarioId, false);
        var scenario = (partnersUnlocked && Random.value < 0.5f) ? partnersScenario : hrScenario;

        var difficulties = new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        var tones = new[] { Tone.Aggressive, Tone.Friendly, Tone.Neutral, Tone.Sarcastic };

        selectedScenario = scenario;
        selectedDifficulty = difficulties[Random.Range(0, difficulties.Length)];
        selectedTone = tones[Random.Range(0, tones.Length)];
        RefreshStatus();
    }

    private void OnBackClicked()
    {
        selectPanelRoot.SetActive(false);
        standardMenuPanelRoot.SetActive(true);
    }

    private void RefreshStatus()
    {
        if (selectedScenario == null)
        {
            statusText.text = "Выберите сценарий";
            return;
        }

        statusText.text = "Сценарий: " + selectedScenario.displayName +
            "\nСложность: " + DifficultyLabel(selectedDifficulty) +
            "   Тон: " + ToneLabel(selectedTone);
    }

    private void OnPlayClicked()
    {
        if (selectedScenario == null) return;

        session.scenario = selectedScenario;
        session.difficulty = selectedDifficulty;
        session.tone = selectedTone;

        selectPanelRoot.SetActive(false);
        battleRoot.SetActive(true);
        battleUI.BeginBattle();
    }

    private static string DifficultyLabel(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: return "Лёгкий";
            case Difficulty.Medium: return "Средний";
            case Difficulty.Hard: return "Сложный";
            default: return "";
        }
    }

    private static string ToneLabel(Tone tone)
    {
        switch (tone)
        {
            case Tone.Aggressive: return "Агрессивный";
            case Tone.Friendly: return "Дружелюбный";
            case Tone.Neutral: return "Нейтральный";
            case Tone.Sarcastic: return "Саркастичный";
            default: return "";
        }
    }
}
