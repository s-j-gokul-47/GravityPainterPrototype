using UnityEngine;

/// <summary>
/// Tracks procedural difficulty from completed levels (PlayerPrefs).
/// Campaign hand-authored levels do not call <see cref="OnLevelCompleted"/>.
/// </summary>
public static class DifficultyManager
{
    public const string KeyLevelsCompleted = "ProceduralLevelsCompleted";
    public const float DefaultDifficultyStep = 0.05f;

    public static int LevelsCompleted => PlayerPrefs.GetInt(KeyLevelsCompleted, 0);

    public static float CurrentDifficulty => Mathf.Clamp01(LevelsCompleted * DefaultDifficultyStep);

    /// <summary>Call once when the ball reaches the procedural finish line.</summary>
    public static void OnLevelCompleted()
    {
        PlayerPrefs.SetInt(KeyLevelsCompleted, LevelsCompleted + 1);
        PlayerPrefs.Save();
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KeyLevelsCompleted);
        PlayerPrefs.Save();
    }

    public static void SetLevelsCompleted(int count)
    {
        PlayerPrefs.SetInt(KeyLevelsCompleted, Mathf.Max(0, count));
        PlayerPrefs.Save();
    }

    public static string GetTierName(float difficulty)
    {
        if (difficulty < 0.25f)
        {
            return "Easy";
        }

        if (difficulty < 0.50f)
        {
            return "Medium";
        }

        if (difficulty < 0.75f)
        {
            return "Hard";
        }

        return "Expert";
    }
}
