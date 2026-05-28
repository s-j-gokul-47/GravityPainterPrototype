#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// CameraFollow on tiles was moving them at Play time. Strip it from anything that is not the main camera.
/// </summary>
public static class RemoveMisplacedCameraFollow
{
    [MenuItem("Gravity Painter/Remove CameraFollow From Tiles (Active Scene)")]
    public static void RemoveFromActiveScene()
    {
        int removed = RemoveMisplaced();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog(
            "CameraFollow cleanup",
            removed > 0
                ? "Removed " + removed + " misplaced CameraFollow component(s).\nOnly Main Camera should keep it."
                : "No misplaced CameraFollow found.",
            "OK");
    }

    public static int RemoveMisplaced()
    {
        int removed = 0;
        CameraFollow[] followers = Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (CameraFollow follower in followers)
        {
            if (follower.GetComponent<Camera>() != null)
            {
                continue;
            }

            Object.DestroyImmediate(follower);
            removed++;
        }

        return removed;
    }
}
#endif
