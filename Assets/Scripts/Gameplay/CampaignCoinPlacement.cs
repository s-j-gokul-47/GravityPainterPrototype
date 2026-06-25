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

    public static void SnapCoinToTile(Transform coinRoot, Transform tile, float extraHeight = 0f)
    {
        if (coinRoot == null || tile == null)
        {
            return;
        }

        float bottomOffset = GetCoinBottomOffset(coinRoot);
        Vector3 position = coinRoot.position;
        position.y = GetTileTopY(tile.gameObject) + bottomOffset + extraHeight;
        coinRoot.position = position;
    }

    public static Transform FindNearestTileForCoin(Transform coinRoot)
    {
        if (coinRoot == null)
        {
            return null;
        }

        TileZone[] zones = Object.FindObjectsByType<TileZone>(FindObjectsSortMode.None);
        Transform best = null;
        float bestDistance = float.MaxValue;

        Vector2 coinXZ = new Vector2(coinRoot.position.x, coinRoot.position.z);
        foreach (TileZone zone in zones)
        {
            if (zone == null)
            {
                continue;
            }

            Vector2 tileXZ = new Vector2(zone.transform.position.x, zone.transform.position.z);
            float distance = (tileXZ - coinXZ).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = zone.transform;
            }
        }

        return best;
    }

    public static float GetTileTopY(GameObject tile)
    {
        float maxY = float.NegativeInfinity;

        Collider[] colliders = tile.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            maxY = Mathf.Max(maxY, collider.bounds.max.y);
        }

        if (maxY > float.NegativeInfinity)
        {
            return maxY;
        }

        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                maxY = Mathf.Max(maxY, renderers[i].bounds.max.y);
            }
        }

        return maxY > float.NegativeInfinity ? maxY : tile.transform.position.y;
    }

    private static float GetCoinBottomOffset(Transform coinRoot)
    {
        Renderer[] renderers = coinRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return SpawnHeightFromProfile;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return coinRoot.position.y - bounds.min.y;
    }

    private static int CompareTilesByName(Transform a, Transform b)
    {
        int left = ParseTileSortKey(a.name);
        int right = ParseTileSortKey(b.name);
        int compare = left.CompareTo(right);
        return compare != 0 ? compare : string.CompareOrdinal(a.name, b.name);
    }
}
