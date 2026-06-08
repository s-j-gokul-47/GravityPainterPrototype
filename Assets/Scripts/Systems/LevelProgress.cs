using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persists which levels are unlocked. UnlockedLevel = highest level number the player may open
/// (e.g. 2 means Level 1 and Level 2 are playable).
/// </summary>
public static class LevelProgress
{
    public const string UnlockedLevelKey = "UnlockedLevel";

    /// <summary>Campaign slot in the level-select menu that launches procedural mode.</summary>
    public const int ProceduralCampaignLevel = 3;

    /// <summary>Scene loaded when the player picks Level 3 (must be in Build Settings).</summary>
    public const string ProceduralSceneName = "Procedural(test)";

    public static int GetUnlockedLevel()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(UnlockedLevelKey, 1));
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        return levelNumber >= 1 && levelNumber <= GetUnlockedLevel();
    }

    /// <summary>
    /// After completing level N, unlock level N+1 (if not already unlocked).
    /// </summary>
    public static void UnlockThrough(int completedLevel)
    {
        if (completedLevel < 1)
        {
            return;
        }

        int nextUnlocked = completedLevel + 1;
        if (nextUnlocked > GetUnlockedLevel())
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, nextUnlocked);
            PlayerPrefs.Save();
        }
    }

    public static int GetLevelNumberFromScene(Scene scene)
    {
        return ParseLevelName(scene.name);
    }

    public static int GetActiveLevelNumber()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (IsProceduralScene(scene))
        {
            return ProceduralCampaignLevel;
        }

        return ParseLevelName(scene.name);
    }

    public static bool IsProceduralScene(Scene scene)
    {
        return scene.name.IndexOf("Procedural", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string GetSceneNameForLevel(int levelNumber)
    {
        if (levelNumber == ProceduralCampaignLevel)
        {
            return ProceduralSceneName;
        }

        return "Level " + levelNumber;
    }

    private static int ParseLevelName(string sceneName)
    {
        const string prefix = "Level ";
        if (!sceneName.StartsWith(prefix))
        {
            return 1;
        }

        if (int.TryParse(sceneName.Substring(prefix.Length), out int level))
        {
            return level;
        }

        return 1;
    }

    public static bool HasBuiltLevel(int levelNumber)
    {
        return Application.CanStreamedLevelBeLoaded(GetSceneNameForLevel(levelNumber));
    }
}
