using UnityEngine;

/// <summary>
/// Maps a 0–1 difficulty value onto runtime <see cref="LevelGenConfig"/> generation fields.
/// Base bounds live on the config asset (<see cref="LevelGenConfig.absoluteMinPath"/> etc.).
/// </summary>
public static class DifficultyScaler
{
    private const float MinGridSize = 7f;
    private const float MaxGridSize = 12f;
    private const float MinTurnFrequency = 0.1f;
    private const float MaxTurnFrequency = 0.7f;

    public static void Apply(LevelGenConfig config, float difficulty)
    {
        if (config == null)
        {
            return;
        }

        difficulty = Mathf.Clamp01(difficulty);
        config.difficulty = difficulty;

        int absoluteMin = Mathf.Max(15, config.absoluteMinPath);
        int absoluteMax = Mathf.Max(absoluteMin + 1, config.absoluteMaxPath);

        config.minPathLength = Mathf.RoundToInt(
            Mathf.Lerp(absoluteMin, absoluteMax * 0.6f, difficulty));
        config.maxPathLength = Mathf.RoundToInt(
            Mathf.Lerp(absoluteMin + 3, absoluteMax, difficulty));
        config.maxPathLength = Mathf.Max(config.minPathLength, config.maxPathLength);

        config.gridWidth = Mathf.RoundToInt(Mathf.Lerp(MinGridSize, MaxGridSize, difficulty));
        config.gridDepth = Mathf.RoundToInt(Mathf.Lerp(MinGridSize, MaxGridSize, difficulty));
        config.gridWidth = Mathf.Max(5, config.gridWidth);
        config.gridDepth = Mathf.Max(5, config.gridDepth);

        config.turnFrequency = Mathf.Lerp(MinTurnFrequency, MaxTurnFrequency, difficulty);

        int gridSpan = Mathf.Max(config.gridWidth, config.gridDepth);
        config.minFinishDistance = gridSpan * 0.4f;
    }
}
