using System;
using UnityEditor;
using UnityEngine;

/// <summary>Keep the three shared FBX exhibits textured, including their detachable viewport parts.</summary>
[InitializeOnLoad]
public class HelmetMaterialImporter : AssetPostprocessor
{
    const string ModelPath = "Assets/Modle/divinghelmet.fbx";
    const string BodyPath = "Assets/Modle/DivingHelmet/Materials/DivingHelmet_Copper.mat";
    const string ViewportPath = "Assets/Modle/DivingHelmet/Materials/DivingHelmet_Viewports.mat";
    const string Marker = "HarbourHelmetPbr-v1";
    static HelmetMaterialImporter() { EditorApplication.delayCall += ConfigureModel; }

    [MenuItem("Tools/Harbour/Reapply helmet textures")]
    public static void ReapplyTextures()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (!importer) return;
        importer.userData = (importer.userData ?? string.Empty).Replace(Marker, string.Empty).Trim();
        ConfigureModel();
    }
    public static void ConfigureModel()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        var body = AssetDatabase.LoadAssetAtPath<Material>(BodyPath);
        var viewports = AssetDatabase.LoadAssetAtPath<Material>(ViewportPath);
        if (!importer || !body || !viewports) return;
        if ((importer.userData ?? string.Empty).Contains(Marker)) return;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "DivingHelmet_MTL"), body);
        importer.userData = (importer.userData + "\n" + Marker).Trim();
        importer.SaveAndReimport();
        Debug.Log("Helmet textures applied to shared divinghelmet FBX; all three exhibits use the same materials.");
    }

    void OnPostprocessModel(GameObject model)
    {
        if (!string.Equals(assetPath, ModelPath, StringComparison.OrdinalIgnoreCase)) return;
        var body = AssetDatabase.LoadAssetAtPath<Material>(BodyPath);
        var viewports = AssetDatabase.LoadAssetAtPath<Material>(ViewportPath);
        if (!body || !viewports) return;
        context.DependsOnSourceAsset(BodyPath); context.DependsOnSourceAsset(ViewportPath);
        int count = 0, windowCount = 0;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            string key = renderer.name.Substring(renderer.name.LastIndexOf('.') + 1).ToLowerInvariant();
            bool viewport = key == "forward" || key == "left" || key == "right" || key == "up";
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++) slots[i] = viewport ? viewports : body;
            renderer.sharedMaterials = slots; count++;
            if (viewport) windowCount++;
        }
        Debug.Log("Helmet PBR import: " + count + " textured renderers, " + windowCount + " transparent viewport parts.");
    }
}
