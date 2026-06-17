using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ApplyThemeSkybox
{
    [MenuItem("Gravity Painter/Apply City Skybox To All Levels")]
    public static void ApplyToAll()
    {
        string texturePath = "Assets/Textures/SkyCityBackground.png";
        string matPath = "Assets/Materials/SkyCitySkybox.mat";

        // 1. Ensure Texture is imported
        AssetDatabase.ImportAsset(texturePath);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureType = TextureImporterType.Default;
            importer.SaveAndReimport();
        }

        Texture2D skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (skyTex == null)
        {
            Debug.LogError($"Could not find texture at {texturePath}");
            return;
        }

        // 2. Ensure Material exists
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (skyMat == null)
        {
            skyMat = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(skyMat, matPath);
        }

        skyMat.shader = Shader.Find("Skybox/Panoramic");
        skyMat.SetTexture("_MainTex", skyTex);
        skyMat.SetFloat("_ImageType", 0); // 360 Degrees
        skyMat.SetFloat("_Mapping", 1); // Latitude Longitude
        skyMat.SetFloat("_Exposure", 1.0f);
        EditorUtility.SetDirty(skyMat);
        AssetDatabase.SaveAssets();

        // 3. Apply to all scenes
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // Find all scenes in the project
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes", "Assets" });
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!System.IO.File.Exists(path))
                {
                    continue;
                }

                if (!path.EndsWith(".unity") || path.Contains("/ThirdParty/"))
                {
                    continue;
                }

                Scene openedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                RenderSettings.skybox = skyMat;
                
                // Remove the brown land/ground plane so the skybox buildings are visible
                foreach (GameObject rootObj in openedScene.GetRootGameObjects())
                {
                    if (rootObj.name == "LevelEnvironment")
                    {
                        Object.DestroyImmediate(rootObj);
                    }
                }

                EditorSceneManager.MarkSceneDirty(openedScene);
                EditorSceneManager.SaveScene(openedScene);
                Debug.Log($"Applied skybox and removed ground from {path}");
            }
            Debug.Log("Finished applying City Skybox Theme and removing ground planes from ALL scenes!");
        }
    }
}
