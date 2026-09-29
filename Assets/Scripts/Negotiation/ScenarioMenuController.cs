using UnityEngine;
using UnityEngine.UI;

public class ScenarioMenuController : MonoBehaviour
{
    public GameObject menuPanelRoot;
    public GameObject freeModePanelRoot;
    public GameObject battleRoot;
    public NegotiationSession session;
    public NegotiationUIController battleUI;

    public ScenarioDefinition hrScenario;
    public ScenarioDefinition partnersScenario;
    public Button hrButton;
    public Button partnersButton;
    public Button customButton;

    private void Awake()
    {
        hrButton.onClick.AddListener(() => Launch(hrScenario));
        partnersButton.onClick.AddListener(() => Launch(partnersScenario));
        customButton.onClick.AddListener(OpenFreeMode);
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
    }

    private void Launch(ScenarioDefinition scenario)
    {
        session.scenario = scenario;
        session.difficulty = scenario.defaultDifficulty;
        session.tone = scenario.defaultTone;

        menuPanelRoot.SetActive(false);
        battleRoot.SetActive(true);
        battleUI.BeginBattle();
    }

    private void OpenFreeMode()
    {
        menuPanelRoot.SetActive(false);
        freeModePanelRoot.SetActive(true);
    }
}
