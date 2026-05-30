using UnityEngine;

/// <summary>
/// Saved reference layout from a tuned tile (e.g. Tile 40). Apply to all tiles via editor menu.
/// </summary>
[CreateAssetMenu(fileName = "TileGlbReferenceLayout", menuName = "Gravity Painter/Tile GLB Reference Layout")]
public class TileGlbReferenceLayoutAsset : ScriptableObject
{
    [Tooltip("Which tile this layout was captured from.")]
    public string sourceTileName = "Tile (40)";

    public TileGlbVisualLayout layout;
}
