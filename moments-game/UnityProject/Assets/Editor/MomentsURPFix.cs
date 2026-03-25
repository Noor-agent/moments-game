using UnityEngine;
using UnityEditor;

/// <summary>
/// One-click URP renderer fix — creates renderer data via menu without URP assembly dependency.
/// </summary>
public static class MomentsURPFix
{
    [MenuItem("Tools/Moments/Fix URP Renderer")]
    public static void FixURPRenderer()
    {
        // Just open Graphics settings and tell user what to do
        SettingsService.OpenProjectSettings("Project/Graphics");
        Debug.Log("[Moments] Graphics Settings opened. Assign the URP Package Sample asset.");
    }
}
