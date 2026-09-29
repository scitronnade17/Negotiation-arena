using UnityEngine;

[CreateAssetMenu(fileName = "ScenarioDefinition", menuName = "Negotiation Arena/Scenario Definition")]
public class ScenarioDefinition : ScriptableObject
{
    public string scenarioId;
    public string displayName;
    public string npcName = "Оппонент";

    [TextArea(2, 8)] public string context;
    [TextArea(2, 8)] public string vulnerability;
    [TextArea(2, 4)] public string openingLine;

    public Tone defaultTone = Tone.Neutral;
    public Difficulty defaultDifficulty = Difficulty.Medium;

    [Tooltip("scenarioId of the scenario this one unlocks on victory. Empty = unlocks nothing.")]
    public string unlocksScenarioId;

    [Header("Visual novel art (optional - leave empty until assets exist for this scenario)")]
    public Sprite background;

    [Tooltip("Player's own portrait while THEY are speaking (sending a message).")]
    public Sprite playerPortrait;
    [Tooltip("Extra rightward nudge for the speaking portrait above, as a fraction of screen width. " +
        "For wide-armed poses (e.g. HR's NeutralHR) it's fine for an outstretched arm to run off the " +
        "screen edge - centered-on-torso reads better than perfectly contained. 0 = no nudge.")]
    public float playerSpeakingPortraitOffsetX = 0f;
    [Tooltip("Player's portrait while listening (NPC is speaking) - picked at random between these two.")]
    public Sprite playerListeningPortraitA;
    public Sprite playerListeningPortraitB;

    [Tooltip("Player's portrait once the battle ends.")]
    public Sprite playerVictoryPortrait;
    public Sprite playerDefeatPortrait;

    public Sprite npcApprovalPortrait;
    public Sprite npcNeutralPortrait;
    public Sprite npcDisapprovalPortrait;

    [Header("Alternative: NPC speaking portrait fixed by the chosen tone for the whole round, " +
        "instead of reacting per-turn to hit_quality. Leave all four empty to keep using the " +
        "reaction-based fields above (e.g. Lead in HR, who visibly softens/hardens as you go).")]
    public Sprite npcAggressiveTonePortrait;
    public Sprite npcFriendlyTonePortrait;
    public Sprite npcNeutralTonePortrait;
    public Sprite npcSarcasticTonePortrait;

    public Sprite GetNpcPortrait(SpriteReaction reaction)
    {
        switch (reaction)
        {
            case SpriteReaction.Approval: return npcApprovalPortrait != null ? npcApprovalPortrait : npcNeutralPortrait;
            case SpriteReaction.Disapproval: return npcDisapprovalPortrait != null ? npcDisapprovalPortrait : npcNeutralPortrait;
            default: return npcNeutralPortrait;
        }
    }

    public Sprite GetNpcSpeakingPortrait(Tone tone, SpriteReaction reaction)
    {
        Sprite toneSprite;
        switch (tone)
        {
            case Tone.Aggressive: toneSprite = npcAggressiveTonePortrait; break;
            case Tone.Friendly: toneSprite = npcFriendlyTonePortrait; break;
            case Tone.Sarcastic: toneSprite = npcSarcasticTonePortrait; break;
            default: toneSprite = npcNeutralTonePortrait; break;
        }

        return toneSprite != null ? toneSprite : GetNpcPortrait(reaction);
    }

    public Sprite GetPlayerListeningPortrait()
    {
        if (playerListeningPortraitA == null) return playerListeningPortraitB;
        if (playerListeningPortraitB == null) return playerListeningPortraitA;
        return Random.value < 0.5f ? playerListeningPortraitA : playerListeningPortraitB;
    }
}
