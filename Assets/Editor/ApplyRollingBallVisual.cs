#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attaches RollingBalls_Sci-fi_4_4 under the gameplay Ball and scales it to match the Unity sphere collider.
/// </summary>
public static class ApplyRollingBallVisual
{
    private const string RollingBallPrefabPath =
        "Assets/Rolling_Balls-Sci-fi_Pack/Rolling_Balls-Sci-fi_Pack_SRP_Build/Prefab/RollingBalls_Sci-fi_4_4.prefab";

    private static readonly string[] LevelScenePaths =
    {
        "Assets/Scenes/Level 1.unity",
        "Assets/Scenes/Level 2.unity",
    };

    [MenuItem("Gravity Painter/Apply Sci-Fi Ball Visual To Selected Ball")]
    public static void ApplyToSelected()
    {
        GameObject ball = Selection.activeGameObject;
        if (ball == null || ball.GetComponent<BallController>() == null)
        {
            EditorUtility.DisplayDialog(
                "Select Ball",
                "Select the Ball object in the Hierarchy (with BallController).",
                "OK");
            return;
        }

        ApplyToBall(ball);
        EditorSceneManager.MarkSceneDirty(ball.scene);
    }

    [MenuItem("Gravity Painter/Apply Sci-Fi Ball Visual To Level 1 And 2")]
    public static void ApplyToLevels()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RollingBallPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Missing prefab", "Could not find:\n" + RollingBallPrefabPath, "OK");
            return;
        }

        string previousScene = SceneManager.GetActiveScene().path;
        int updated = 0;

        foreach (string scenePath in LevelScenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BallController[] balls = Object.FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (BallController ball in balls)
            {
                ApplyToBall(ball.gameObject, prefab);
                updated++;
            }

            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(previousScene) && System.IO.File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Sci-Fi ball applied",
            "Updated " + updated + " Ball object(s) in Level 1 and Level 2.\n\n" +
            "Physics: same SphereCollider (radius 0.5).\n" +
            "Visual: RollingBalls_Sci-fi_4_4 scaled to diameter 1.",
            "OK");
    }

    private static void ApplyToBall(GameObject ball)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RollingBallPrefabPath);
        ApplyToBall(ball, prefab);
    }

    private static void ApplyToBall(GameObject ball, GameObject prefab)
    {
        BallController controller = ball.GetComponent<BallController>();
        if (controller == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("useSciFiBallVisual").boolValue = true;
        so.FindProperty("sciFiBallVisualPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        Transform oldVisual = ball.transform.Find("SciFiBallVisual");
        if (oldVisual != null)
        {
            Object.DestroyImmediate(oldVisual.gameObject);
        }

        controller.EnsureSciFiBallVisual();

        MeshRenderer rootRenderer = ball.GetComponent<MeshRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        EditorUtility.SetDirty(ball);
        Debug.Log("Ball: RollingBalls_Sci-fi_4_4 visual aligned to sphere collider on " + ball.name + ".");
    }
}
#endif
