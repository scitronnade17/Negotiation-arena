using UnityEngine;

public static class ScenarioProgress
{
    private const string KeyPrefix = "scenario_unlocked_";

    public static bool IsUnlocked(string scenarioId, bool unlockedByDefault)
    {
        if (unlockedByDefault) return true;
        if (string.IsNullOrEmpty(scenarioId)) return true;
        return PlayerPrefs.GetInt(KeyPrefix + scenarioId, 0) == 1;
    }

    public static void Unlock(string scenarioId)
    {
        if (string.IsNullOrEmpty(scenarioId)) return;
        PlayerPrefs.SetInt(KeyPrefix + scenarioId, 1);
        PlayerPrefs.Save();
    }
}
