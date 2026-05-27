using UnityEngine;

/// <summary>
/// Editor-only: replaces the placeholder cube with hammer.glb when the scene loads.
/// </summary>
[ExecuteAlways]
public class HammerModelBinder : MonoBehaviour
{
    [SerializeField] private bool bindInEditor = true;

    private void OnEnable()
    {
        if (!bindInEditor || Application.isPlaying)
        {
            return;
        }

        UnityEditor.EditorApplication.delayCall += TryBindDelayed;
    }

    private void OnDisable()
    {
        UnityEditor.EditorApplication.delayCall -= TryBindDelayed;
    }

    private void TryBindDelayed()
    {
        if (this == null || !bindInEditor || Application.isPlaying)
        {
            return;
        }

        HammerModelUtility.TryUpgradeHammerHierarchy(transform);
    }
}
