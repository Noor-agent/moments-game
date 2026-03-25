using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-click fix: creates a proper URP Forward Renderer asset and assigns it
/// to the active URP Pipeline Asset. Fixes pink materials caused by missing renderer data.
/// Menu: Tools → Moments → Fix URP Renderer
/// </summary>
public static class MomentsURPFix
{
    [MenuItem("Tools/Moments/Fix URP Renderer")]
    public static void FixURPRenderer()
    {
        // Find the active URP asset
        var urpAsset = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline
            as UniversalRenderPipelineAsset;

        if (urpAsset == null)
        {
            EditorUtility.DisplayDialog("❌ No URP Asset",
                "No Universal Render Pipeline asset is assigned in Graphics Settings.\n\n" +
                "Go to: Edit → Project Settings → Graphics → assign the URP asset first.",
                "OK");
            return;
        }

        // Create a Forward Renderer Data asset
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        rendererData.name = "MomentsForwardRenderer";

        string path = "Assets/Settings/MomentsForwardRenderer.asset";
        AssetDatabase.CreateAsset(rendererData, path);
        AssetDatabase.SaveAssets();

        // Assign it to the URP asset
        urpAsset.SetRenderer(0, rendererData);
        EditorUtility.SetDirty(urpAsset);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("✅ URP Renderer Fixed!",
            "Created MomentsForwardRenderer and assigned it to the URP Pipeline Asset.\n\n" +
            "Pink materials should now be fixed!\n\n" +
            "Press Play to test.",
            "Let's go!");

        Debug.Log("[Moments] ✅ URP Forward Renderer created and assigned.");
    }
}
