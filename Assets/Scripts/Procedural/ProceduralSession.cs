using UnityEngine;

/// <summary>
/// Resolves procedural layout seeds. Seeds are derived only from the menu level number
/// so every install produces the same path for Level 3, Level 4, etc.
/// </summary>
public static class ProceduralSession
{
    /// <summary>Kept for callers; seeds are always deterministic from menu level.</summary>
    public static void MarkFreshRunFromMenu()
    {
    }

    /// <summary>
    /// Fixed seed for a campaign menu slot. Same formula on every device and build.
    /// Level 3 → first procedural layout, Level 4 → next, and so on.
    /// </summary>
    public static int GetDeterministicSeedForMenuLevel(int menuLevel)
    {
        if (menuLevel < LevelProgress.ProceduralCampaignLevel)
        {
            menuLevel = LevelProgress.ProceduralCampaignLevel;
        }

        unchecked
        {
            const int salt = 0x5F3759DF;
            uint hash = (uint)(menuLevel * 100003 ^ salt);
            return (int)(hash % (int.MaxValue - 1)) + 1;
        }
    }

    public static int ResolveStartSeed(int sceneDefaultSeed)
    {
        int menuLevel = LevelProgress.GetSelectedMenuLevel();
        if (LevelProgress.IsProceduralMenuLevel(menuLevel))
        {
            return GetDeterministicSeedForMenuLevel(menuLevel);
        }

        if (sceneDefaultSeed > 0)
        {
            return sceneDefaultSeed;
        }

        return GetDeterministicSeedForMenuLevel(LevelProgress.ProceduralCampaignLevel);
    }

    public static int CreateMenuEntrySeed()
    {
        return GetDeterministicSeedForMenuLevel(LevelProgress.GetSelectedMenuLevel());
    }
}
