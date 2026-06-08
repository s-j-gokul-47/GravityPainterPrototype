#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor shortcuts for tuning procedural difficulty during playtests.
/// </summary>
public static class DifficultyEditorMenu
{
    [MenuItem("Gravity Painter/Reset Difficulty Progress")]
    public static void ResetDifficulty()
    {
        DifficultyManager.ResetProgress();
        Debug.Log(
            "Procedural difficulty reset. Levels completed: "
            + DifficultyManager.LevelsCompleted
            + ", difficulty: "
            + DifficultyManager.CurrentDifficulty.ToString("F2"));
    }

    [MenuItem("Gravity Painter/Force Easy Difficulty (0 completed)")]
    public static void ForceEasy()
    {
        DifficultyManager.SetLevelsCompleted(0);
        LogForcedTier();
    }

    [MenuItem("Gravity Painter/Force Medium Difficulty (5 completed)")]
    public static void ForceMedium()
    {
        DifficultyManager.SetLevelsCompleted(5);
        LogForcedTier();
    }

    [MenuItem("Gravity Painter/Force Hard Difficulty (10 completed)")]
    public static void ForceHard()
    {
        DifficultyManager.SetLevelsCompleted(10);
        LogForcedTier();
    }

    [MenuItem("Gravity Painter/Force Expert Difficulty (20 completed)")]
    public static void ForceExpert()
    {
        DifficultyManager.SetLevelsCompleted(20);
        LogForcedTier();
    }

    private static void LogForcedTier()
    {
        Debug.Log(
            "Forced procedural difficulty: "
            + DifficultyManager.GetTierName(DifficultyManager.CurrentDifficulty)
            + " ("
            + DifficultyManager.CurrentDifficulty.ToString("F2")
            + ", "
            + DifficultyManager.LevelsCompleted
            + " levels completed). Rebuild the procedural level in Play mode.");
    }
}
#endif
