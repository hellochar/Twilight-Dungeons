using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-time setup script to configure URP 2D rendering pipeline.
/// Run via menu: Tools > Setup URP 2D Pipeline
/// Safe to delete after setup is complete.
/// </summary>
public static class URPSetup
{
    [MenuItem("Tools/Setup URP 2D Pipeline")]
    public static void SetupURP2D()
    {
        // Ensure Settings directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        // Create 2D Renderer Data asset
        var renderer2D = ScriptableObject.CreateInstance<Renderer2DData>();
        AssetDatabase.CreateAsset(renderer2D, "Assets/Settings/Renderer2D.asset");

        // Create URP Asset referencing the 2D renderer
        var urpAsset = UniversalRenderPipelineAsset.Create(renderer2D);
        AssetDatabase.CreateAsset(urpAsset, "Assets/Settings/URPAsset-2D.asset");

        // Assign as active render pipeline
        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline = urpAsset;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("URP 2D Pipeline configured. Assets created in Assets/Settings/.");
        Debug.Log("Next steps:");
        Debug.Log("  1. Run Window > Rendering > Render Pipeline Converter");
        Debug.Log("  2. Select 'Built-in to 2D (URP)' and convert materials");
        Debug.Log("  3. Add lighting via Tools > Setup Basic 2D Lighting");

        EditorUtility.DisplayDialog(
            "URP 2D Setup Complete",
            "Pipeline assets created in Assets/Settings/.\n\n" +
            "Next: Run Window > Rendering > Render Pipeline Converter\n" +
            "Select 'Built-in to 2D (URP)' to convert materials.",
            "OK"
        );
    }

    [MenuItem("Tools/Setup Basic 2D Lighting")]
    public static void SetupBasic2DLighting()
    {
        // Add a dim Global Light 2D for ambient
        var ambientGO = new GameObject("Global Light 2D - Ambient");
        var globalLight = ambientGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.15f, 0.12f, 0.2f); // dark purple-blue dungeon ambient
        globalLight.intensity = 0.3f;

        // Add a sample point light the user can duplicate/move
        var pointGO = new GameObject("Point Light 2D - Sample Torch");
        var pointLight = pointGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        pointLight.lightType = Light2D.LightType.Point;
        pointLight.color = new Color(1f, 0.8f, 0.4f); // warm torch color
        pointLight.intensity = 1.5f;
        pointLight.pointLightOuterRadius = 5f;
        pointLight.pointLightInnerRadius = 1f;

        Undo.RegisterCreatedObjectUndo(ambientGO, "Create Global Light 2D");
        Undo.RegisterCreatedObjectUndo(pointGO, "Create Point Light 2D");

        Debug.Log("Basic 2D lighting created. Adjust colors/intensity to taste.");
        Debug.Log("Duplicate the Point Light and place on torches, doorways, etc.");
    }
}
