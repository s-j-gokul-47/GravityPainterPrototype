using UnityEngine;

/// <summary>
/// Saved local transforms for TileGlbVisualRoot and TilesGlbMesh (tune one tile, copy to all).
/// </summary>
[System.Serializable]
public struct TileGlbVisualLayout
{
    public Vector3 rootLocalPosition;
    public Quaternion rootLocalRotation;
    public Vector3 rootLocalScale;

    public Vector3 meshLocalPosition;
    public Quaternion meshLocalRotation;
    public Vector3 meshLocalScale;

    public bool autoAlignStripLayoutToTile;
    public Vector3 modelEuler;
}
