using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility: registers all 12 Moments scenes in Build Settings automatically.
/// Menu: Tools → Moments → Setup Build Scenes
/// Also runs automatically the first time the project is opened (via InitializeOnLoad).
/// </summary>
[InitializeOnLoad]
public static class MomentsSceneSetup
{
    // ── Scene paths in required load order ────────────────────────────────
    private static readonly string[] ScenePaths = new[]
    {
        "Assets/Scenes/Bootstrap.unity",
        "Assets/Scenes/Attract.unity",
        "Assets/Scenes/Lobby.unity",
        "Assets/Scenes/Results.unity",
        "Assets/Scenes/Podium.unity",
        "Assets/Scenes/MiniGames/PolarPush.unity",
        "Assets/Scenes/MiniGames/ColorClash.unity",
        "Assets/Scenes/MiniGames/TankBattle.unity",
        "Assets/Scenes/MiniGames/WaveRider.unity",
        "Assets/Scenes/MiniGames/BumperBlitz.unity",
        "Assets/Scenes/MiniGames/BlinkShot.unity",
        "Assets/Scenes/MiniGames/GravityGrab.unity",
    };

    // ── Auto-run on project open ───────────────────────────────────────────
    static MomentsSceneSetup()
    {
        // Defer until after Unity finishes domain reload
        EditorApplication.delayCall += AutoSetupIfNeeded;
    }

    private static void AutoSetupIfNeeded()
    {
        var current = EditorBuildSettings.scenes;

        // Already set up correctly → skip silently
        if (current.Length == ScenePaths.Length)
        {
            bool allMatch = true;
            for (int i = 0; i < current.Length; i++)
                if (current[i].path != ScenePaths[i]) { allMatch = false; break; }
            if (allMatch) return;
        }

        // Missing or wrong → auto-register and notify
        int registered = RegisterScenes();
        if (registered > 0)
            Debug.Log($"[Moments] ✅ Auto-registered {registered} scenes in Build Settings.");
    }

    // ── Menu item ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Moments/Setup Build Scenes")]
    public static void SetupBuildScenes()
    {
        int registered = RegisterScenes();

        if (registered == ScenePaths.Length)
        {
            EditorUtility.DisplayDialog(
                "✅ Moments — Build Scenes Ready",
                $"All {registered} scenes registered successfully!\n\n" +
                "Scene order:\n" +
                "0 · Bootstrap\n" +
                "1 · Attract\n" +
                "2 · Lobby\n" +
                "3 · Results\n" +
                "4 · Podium\n" +
                "5 · PolarPush\n" +
                "6 · ColorClash\n" +
                "7 · TankBattle\n" +
                "8 · WaveRider\n" +
                "9 · BumperBlitz\n" +
                "10 · BlinkShot\n" +
                "11 · GravityGrab\n\n" +
                "You're ready to press Play!",
                "Great!");
        }
        else
        {
            int missing = ScenePaths.Length - registered;
            EditorUtility.DisplayDialog(
                "⚠️ Moments — Some Scenes Missing",
                $"Registered {registered}/{ScenePaths.Length} scenes.\n" +
                $"{missing} scene file(s) not found on disk.\n\n" +
                "Missing scenes were skipped. " +
                "Check the Console for details.",
                "OK");
        }
    }

    // ── Core logic ────────────────────────────────────────────────────────
    private static int RegisterScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        int found  = 0;

        foreach (var path in ScenePaths)
        {
            // Check scene file actually exists on disk
            string fullPath = System.IO.Path.Combine(
                Application.dataPath.Replace("/Assets", ""), path);

            if (System.IO.File.Exists(fullPath))
            {
                scenes.Add(new EditorBuildSettingsScene(path, enabled: true));
                found++;
            }
            else
            {
                Debug.LogWarning($"[Moments] Scene not found, skipped: {path}");
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        return found;
    }

    // ── Validation menu item (shows current state) ────────────────────────
    [MenuItem("Tools/Moments/Check Build Scenes")]
    public static void CheckBuildScenes()
    {
        var current = EditorBuildSettings.scenes;
        var lines   = new System.Text.StringBuilder();

        lines.AppendLine($"Build Settings has {current.Length} scene(s):\n");

        for (int i = 0; i < current.Length; i++)
            lines.AppendLine($"  {i} · {current[i].path}");

        if (current.Length == 0)
            lines.AppendLine("  (none — run Tools → Moments → Setup Build Scenes)");

        Debug.Log("[Moments] " + lines);
        EditorUtility.DisplayDialog("Moments — Current Build Scenes", lines.ToString(), "OK");
    }
}
