using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Shared coin placement rules for campaign levels and the editor placement tool.
/// </summary>
public static class CampaignCoinPlacement
{
    public const float SpawnHeight = 0.8f;
    public const float SpawnChance = 0.25f;

    public static Vector3 SpawnScale => CoinAppearanceProfile.LoadOrDefault().rootLocalScale;

    public static float SpawnHeightFromProfile => CoinAppearanceProfile.LoadOrDefault().spawnHeight;

    public static int SeedForLevel(int levelNumber)
    {
        return 1000 + levelNumber;
    }

    public static bool ShouldSpawnCoin(int placementSeed, int tileSortKey)
    {
        Random.State oldState = Random.state;
        Random.InitState(placementSeed + tileSortKey * 73);
        bool spawn = Random.value <= SpawnChance;
        Random.state = oldState;
        return spawn;
    }

    public static Quaternion RandomSpawnRotation(int placementSeed, int tileSortKey)
    {
        Random.State oldState = Random.state;
        Random.InitState(placementSeed + tileSortKey * 73 + 17);
        float yaw = Random.Range(0f, 360f);
        Random.state = oldState;
        return Quaternion.Euler(0f, yaw, 0f);
    }

    public static List<Transform> CollectEligibleTiles(Transform ball, FinishLine finishLine)
    {
        TileZone[] zones = Object.FindObjectsByType<TileZone>(FindObjectsSortMode.None);
        var tiles = new List<Transform>(zones.Length);
        foreach (TileZone zone in zones)
        {
            if (zone == null)
            {
                continue;
            }

            Transform tile = zone.transform;
            if (finishLine != null && tile == finishLine.transform)
            {
                continue;
            }

            tiles.Add(tile);
        }

        tiles.Sort(CompareTilesByName);

        if (tiles.Count > 0 && ball != null)
        {
            int startIndex = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < tiles.Count; i++)
            {
                float distance = Vector3.SqrMagnitude(tiles[i].position - ball.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    startIndex = i;
                }
            }

            tiles.RemoveAt(startIndex);
        }

        return tiles;
    }

    public static int ParseTileSortKey(string tileName)
    {
        Match match = Regex.Match(tileName ?? string.Empty, @"\((\d+)\)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
        {
            return value;
        }

        return tileName != null ? tileName.GetHashCode() : 0;
    }

    private static int CompareTilesByName(Transform a, Transform b)
    {
        int left = ParseTileSortKey(a.name);
        int right = ParseTileSortKey(b.name);
        int compare = left.CompareTo(right);
        return compare != 0 ? compare : string.CompareOrdinal(a.name, b.name);
    }
}
