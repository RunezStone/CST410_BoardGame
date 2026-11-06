using System.Collections.Generic;
using System.IO;
using Convergence.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Convergence > Build 4 > ...
public static class Build4Tools
{
    const string Folder = "Assets/Ysabelle/Build4";
    const string AssetPath = Folder + "/Resources/DiceLab.asset";
    const string ScenePath = Folder + "/Build4Driver.unity";

    [MenuItem("Convergence/Build 4/Create Dice Lab Asset and Driver Scene")]
    public static void CreateAll()
    {
        var lab = AssetDatabase.LoadAssetAtPath<DiceLabConfig>(AssetPath);
        if (lab == null)
        {
            Directory.CreateDirectory(Folder + "/Resources");
            lab = ScriptableObject.CreateInstance<DiceLabConfig>();
            AssetDatabase.CreateAsset(lab, AssetPath);
            AssetDatabase.SaveAssets();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        }
        var go = new GameObject("Build 4 Turn Driver");
        var driver = go.AddComponent<TurnDriver>();
        var so = new SerializedObject(driver);
        so.FindProperty("diceLab").objectReferenceValue = lab;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Created {AssetPath} and {ScenePath}");
    }

    // Plays the same run with no scene, twice, and checks both runs are identical (repeatable seed).
    [MenuItem("Convergence/Build 4/Run Headless (2 rounds, twice)")]
    public static void RunHeadless()
    {
        var lab = DiceLabConfig.Load();
        string a = RunOnce(lab, lab.seed);
        string b = RunOnce(lab, lab.seed);
        string c = RunOnce(lab, lab.seed + 1);

        File.WriteAllText("Build4_HeadlessRun.txt", a);
        Debug.Log(a);
        Debug.Log(a == b ? "REPEATABLE: two runs with the same seed produced identical logs."
                         : "NOT REPEATABLE: same seed gave different logs!");
        Debug.Log(a != c ? "Different seed -> different game (as expected)." : "Different seed gave the same game?");
        if (a != b) Debug.LogError("Build 4 headless run is not repeatable.");
    }

    static string RunOnce(DiceLabConfig lab, int seed)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var _ in TurnDriver.Run(lab, seed, 2, true, line => sb.AppendLine(line))) { }
        return sb.ToString();
    }

    // For command-line use:  Unity -batchmode -projectPath . -executeMethod Build4Tools.Batch -quit
    public static void Batch()
    {
        CreateAll();
        RunHeadless();
    }
}
