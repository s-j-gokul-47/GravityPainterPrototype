using UnityEngine;

/// <summary>
/// Persists the active procedural seed so scene reloads and menu launches use the right layout.
/// </summary>
public static class ProceduralSession
{
    public const string SavedSeedKey = "ProceduralSavedSeed";
    public const string HasSavedSeedKey = "ProceduralHasSavedSeed";
    public const string FreshRunKey = "ProceduralFreshRun";

    public static void MarkFreshRunFromMenu()
    {
        PlayerPrefs.SetInt(FreshRunKey, 1);
        PlayerPrefs.DeleteKey(HasSavedSeedKey);
        PlayerPrefs.Save();
    }

    public static void SaveSeed(int seed)
    {
        if (seed <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(SavedSeedKey, seed);
        PlayerPrefs.SetInt(HasSavedSeedKey, 1);
        PlayerPrefs.DeleteKey(FreshRunKey);
        PlayerPrefs.Save();
    }

    public static bool TryGetSavedSeed(out int seed)
    {
        seed = 0;
        if (PlayerPrefs.GetInt(HasSavedSeedKey, 0) != 1)
        {
            return false;
        }

        seed = PlayerPrefs.GetInt(SavedSeedKey, 0);
        return seed > 0;
    }

    public static int ResolveStartSeed(int sceneDefaultSeed)
    {
        if (PlayerPrefs.GetInt(FreshRunKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(FreshRunKey);
            PlayerPrefs.Save();
            return CreateMenuEntrySeed();
        }

        if (TryGetSavedSeed(out int savedSeed))
        {
            return savedSeed;
        }

        if (sceneDefaultSeed > 0)
        {
            return sceneDefaultSeed;
        }

        return CreateMenuEntrySeed();
    }

    public static int CreateMenuEntrySeed()
    {
        int menuLevel = LevelProgress.GetSelectedMenuLevel();
        unchecked
        {
            return (menuLevel * 100003) ^ Random.Range(1, int.MaxValue / 2);
        }
    }
}
