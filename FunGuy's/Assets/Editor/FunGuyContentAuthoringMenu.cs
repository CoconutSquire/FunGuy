using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FunGuyContentAuthoringMenu
{
    private const string AssetFolder = "Assets/_Game/Resources/GameData";
    private const string AssetPath = AssetFolder + "/UnityContentCatalog.asset";

    [MenuItem("FunGuy/Content/Create Unity Catalog From JSON")]
    public static void CreateOrRefreshCatalog()
    {
        EnsureFolder("Assets/_Game");
        EnsureFolder("Assets/_Game/Resources");
        EnsureFolder(AssetFolder);

        var catalog = AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(AssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ContentCatalogAsset>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }

        catalog.characters = JsonLoader.LoadFromResources<CharactersFile>("GameData/characters");
        catalog.skills = JsonLoader.LoadFromResources<SkillsFile>("GameData/skills");
        catalog.stages = JsonLoader.LoadFromResources<StagesFile>("GameData/stages");
        catalog.banners = JsonLoader.LoadFromResources<BannersFile>("GameData/banners");
        catalog.schemaVersion = 1;
        catalog.contentVersion = "unity-authored-v1";
        catalog.ValidateSchema();

        var statRules = new StatRulesCatalog(JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules"));
        GameDataValidator.Validate(catalog.characters, catalog.skills, catalog.stages, catalog.banners, statRules);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = catalog;
        Debug.Log($"Created Unity-authored content catalog at {AssetPath}. Edit it in the Inspector.");
    }

    [MenuItem("FunGuy/Content/Validate Unity Catalog")]
    public static void ValidateCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(AssetPath);
        if (catalog == null) throw new InvalidOperationException($"Catalog not found. Run FunGuy > Content > Create Unity Catalog From JSON first.");
        catalog.ValidateSchema();
        var statRules = new StatRulesCatalog(JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules"));
        GameDataValidator.Validate(catalog.characters, catalog.skills, catalog.stages, catalog.banners, statRules);
        Debug.Log($"Unity catalog valid: {catalog.characters.characters.Count} characters, {catalog.skills.skills.Count} skills, {catalog.stages.stages.Count} stages, {catalog.banners.banners.Count} banners.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }
}
