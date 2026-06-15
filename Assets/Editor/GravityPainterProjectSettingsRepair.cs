#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Keeps critical ProjectSettings stable across Unity restarts.
/// Deleting ProjectSettings makes Unity regenerate broken defaults:
/// - activeInputHandler = 0 → tiles ignore taps (Input System code never runs)
/// - missing input actions config → same symptom
/// - no URP pipeline → pink materials
/// - wrong build scenes → levels do not load on device
/// </summary>
[InitializeOnLoad]
public static class GravityPainterProjectSettingsRepair
{
    private const string MenuRepairPath = "Gravity Painter/Repair Project Settings";
    private const string MenuDiagnosePath = "Gravity Painter/Diagnose Project Settings";

    private const string PcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
    private const string InputActionsPath = "Assets/Settings/Input/InputSystem_Actions.inputactions";
    private const string InputSettingsKey = "com.unity.input.settings.actions";
    private const string ActiveInputHandlerProperty = "activeInputHandler";
    private const string ProjectVersionPath = "ProjectSettings/ProjectVersion.txt";

    private const string RequiredEditorVersion = "6000.3.11f1";
    private const string RequiredEditorRevision = "6000.3.11f1 (3000ef702840)";

    // 0 = old Input Manager only (breaks tile painting), 1 = Input System, 2 = both (required).
    private const int InputHandlerBoth = 2;

    private static readonly (string path, bool enabled)[] RequiredScenes =
    {
        ("Assets/Scenes/Menus/MainMenu.unity", true),
        ("Assets/Scenes/Levels/Level 1.unity", true),
        ("Assets/Scenes/Levels/Level 2.unity", true),
        ("Assets/Procedural(test).unity", true),
        ("Assets/Scenes/Levels/Level 3.unity", false),
        ("Assets/Scenes/Levels/Level 4.unity", false),
        ("Assets/Scenes/Levels/Level 5.unity", false),
    };

    static GravityPainterProjectSettingsRepair()
    {
        EditorApplication.delayCall += OnEditorLoadPass1;
    }

    [MenuItem(MenuRepairPath)]
    public static void RepairFromMenu()
    {
        bool changed = RepairInternal(logSummary: true);
        EditorUtility.DisplayDialog(
            "Project Settings",
            changed
                ? "Repaired ProjectSettings. Check the Console for details."
                : "ProjectSettings already look correct.",
            "OK");
    }

    [MenuItem(MenuDiagnosePath)]
    public static void DiagnoseFromMenu()
    {
        Debug.Log(BuildDiagnosticReport());
    }

    private static void OnEditorLoadPass1()
    {
        string report = BuildDiagnosticReport();
        if (NeedsRepair(out List<string> issues))
        {
            Debug.LogWarning(
                "[Gravity Painter] ProjectSettings need repair before Play will work:\n"
                + report);
            RepairInternal(logSummary: true);
        }
        else
        {
            Debug.Log("[Gravity Painter] ProjectSettings OK.\n" + report);
        }

        // PlayerSettings singleton may not be ready on the first delayCall after a fresh ProjectSettings folder.
        EditorApplication.delayCall += OnEditorLoadPass2;
    }

    private static void OnEditorLoadPass2()
    {
        if (!NeedsRepair(out _))
        {
            return;
        }

        Debug.Log("[Gravity Painter] Running second ProjectSettings repair pass...");
        RepairInternal(logSummary: true);
    }

    private static bool NeedsRepair(out List<string> issues)
    {
        issues = new List<string>();
        CollectIssues(issues);
        return issues.Count > 0;
    }

    private static bool RepairInternal(bool logSummary)
    {
        List<string> issues = new List<string>();
        CollectIssues(issues);
        if (issues.Count == 0)
        {
            return false;
        }

        bool changed = false;
        changed |= RepairProjectVersion();
        changed |= RepairBuildScenes();
        changed |= RepairRenderPipeline();
        changed |= RepairInputSystem();
        changed |= RepairInputActionsConfig();

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }

        if (logSummary)
        {
            Debug.Log(
                "[Gravity Painter] Repaired ProjectSettings: "
                + string.Join("; ", issues)
                + "\n"
                + BuildDiagnosticReport());
        }

        return changed;
    }

    private static string BuildDiagnosticReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Gravity Painter] ProjectSettings diagnostic:");

        sb.AppendLine("  Editor version: " + ReadProjectVersionLine());
        sb.AppendLine("  activeInputHandler: " + DescribeInputHandler(GetActiveInputHandler()));
        sb.AppendLine("  Input actions wired: " + InputActionsConfigMatches());
        sb.AppendLine("  URP pipeline: " + DescribePipeline(GraphicsSettings.defaultRenderPipeline, PcPipelinePath));
        sb.AppendLine("  Build scenes OK: " + BuildScenesMatch());
        sb.AppendLine("  Scenes in build:");
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes ?? new EditorBuildSettingsScene[0])
        {
            sb.AppendLine("    [" + (scene.enabled ? "x" : " ") + "] " + scene.path);
        }

        return sb.ToString();
    }

    private static string ReadProjectVersionLine()
    {
        if (!File.Exists(ProjectVersionPath))
        {
            return "(missing ProjectVersion.txt)";
        }

        return File.ReadAllLines(ProjectVersionPath).FirstOrDefault() ?? "(empty)";
    }

    private static string DescribeInputHandler(int value)
    {
        switch (value)
        {
            case 0: return "0 (OLD Input Manager only — tile taps will NOT work)";
            case 1: return "1 (Input System)";
            case 2: return "2 (Both — correct)";
            default: return value + " (unexpected)";
        }
    }

    private static string DescribePipeline(RenderPipelineAsset current, string expectedPath)
    {
        RenderPipelineAsset expected = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(expectedPath);
        if (current == null)
        {
            return "MISSING (pink materials)";
        }

        if (expected != null && current == expected)
        {
            return "OK (" + expectedPath + ")";
        }

        return "WRONG (" + current.name + ")";
    }

    private static void CollectIssues(List<string> issues)
    {
        if (!ProjectVersionMatches())
        {
            issues.Add("editor version");
        }

        if (!BuildScenesMatch())
        {
            issues.Add("build scenes");
        }

        if (!RenderPipelineMatches())
        {
            issues.Add("URP pipeline");
        }

        int inputHandler = GetActiveInputHandler();
        if (inputHandler != 1 && inputHandler != 2)
        {
            issues.Add("input handler (" + inputHandler + ")");
        }

        if (!InputActionsConfigMatches())
        {
            issues.Add("input actions");
        }
    }

    private static bool ProjectVersionMatches()
    {
        if (!File.Exists(ProjectVersionPath))
        {
            return false;
        }

        string text = File.ReadAllText(ProjectVersionPath);
        return text.Contains(RequiredEditorVersion);
    }

    private static bool RepairProjectVersion()
    {
        if (ProjectVersionMatches())
        {
            return false;
        }

        string contents = "m_EditorVersion: " + RequiredEditorVersion + "\n"
            + "m_EditorVersionWithRevision: " + RequiredEditorRevision + "\n";
        File.WriteAllText(ProjectVersionPath, contents);
        return true;
    }

    private static bool BuildScenesMatch()
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (current == null || current.Length == 0)
        {
            return false;
        }

        foreach ((string path, bool enabled) required in RequiredScenes)
        {
            EditorBuildSettingsScene match = current.FirstOrDefault(scene => scene.path == required.path);
            if (match == null || match.enabled != required.enabled)
            {
                return false;
            }
        }

        return current.Any(scene => scene.path == "Assets/Procedural(test).unity" && scene.enabled);
    }

    private static bool RepairBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        foreach ((string path, bool enabled) required in RequiredScenes)
        {
            scenes.Add(new EditorBuildSettingsScene(required.path, required.enabled));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        return true;
    }

    private static bool RenderPipelineMatches()
    {
        RenderPipelineAsset expected = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PcPipelinePath);
        return expected != null && GraphicsSettings.defaultRenderPipeline == expected;
    }

    private static bool RepairRenderPipeline()
    {
        RenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PcPipelinePath);
        if (pipeline == null)
        {
            Debug.LogWarning("[Gravity Painter] Missing URP asset at " + PcPipelinePath);
            return false;
        }

        if (GraphicsSettings.defaultRenderPipeline == pipeline)
        {
            return false;
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        return true;
    }

    private static int GetActiveInputHandler()
    {
        SerializedProperty property = FindActiveInputHandlerProperty();
        if (property == null)
        {
            return -1;
        }

        return property.intValue;
    }

    private static bool SetActiveInputHandler(int value)
    {
        Object settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
        if (settings == null)
        {
            return false;
        }

        SerializedObject serializedObject = new SerializedObject(settings);
        SerializedProperty property = serializedObject.FindProperty(ActiveInputHandlerProperty);
        if (property == null || property.intValue == value)
        {
            return false;
        }

        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static SerializedProperty FindActiveInputHandlerProperty()
    {
        Object settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
        if (settings == null)
        {
            return null;
        }

        SerializedObject serializedObject = new SerializedObject(settings);
        return serializedObject.FindProperty(ActiveInputHandlerProperty);
    }

    private static bool RepairInputSystem()
    {
        int current = GetActiveInputHandler();
        if (current == 1 || current == 2)
        {
            return false;
        }

        return SetActiveInputHandler(InputHandlerBoth);
    }

    private static bool InputActionsConfigMatches()
    {
        Object actions = AssetDatabase.LoadAssetAtPath<Object>(InputActionsPath);
        if (actions == null)
        {
            return true;
        }

        if (!EditorBuildSettings.TryGetConfigObject(InputSettingsKey, out Object configured))
        {
            return false;
        }

        return configured == actions;
    }

    private static bool RepairInputActionsConfig()
    {
        Object actions = AssetDatabase.LoadAssetAtPath<Object>(InputActionsPath);
        if (actions == null)
        {
            return false;
        }

        if (EditorBuildSettings.TryGetConfigObject(InputSettingsKey, out Object configured)
            && configured == actions)
        {
            return false;
        }

        EditorBuildSettings.AddConfigObject(InputSettingsKey, actions, true);
        return true;
    }
}
#endif
