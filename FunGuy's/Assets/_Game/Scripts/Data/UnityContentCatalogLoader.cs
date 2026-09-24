using System;
using UnityEngine;

public static class UnityContentCatalogLoader
{
    public const string ResourcesPath = "GameData/UnityContentCatalog";

    public static bool TryLoad(out ContentCatalogAsset asset)
    {
        asset = Resources.Load<ContentCatalogAsset>(ResourcesPath);
        if (asset == null) return false;
        asset.ValidateSchema();
        return true;
    }

    public static void ValidateAsset(ContentCatalogAsset asset)
    {
        if (asset == null) throw new ArgumentNullException(nameof(asset));
        var snapshot = CreateSnapshot(asset);
        var statRules = new StatRulesCatalog(JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules"));
        GameDataValidator.Validate(snapshot.characters, snapshot.skills, snapshot.stages, snapshot.banners, statRules);
    }

    public static void LoadInto(GameData data, ContentCatalogAsset asset)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (asset == null) throw new ArgumentNullException(nameof(asset));

        // The ScriptableObject is authored source, not mutable runtime state.
        // JsonUtility gives us detached DTO graphs including nested stage waves/effects.
        var snapshot = CreateSnapshot(asset);
        var statRules = new StatRulesCatalog(JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules"));
        var levelRules = new LevelProgressionRules(JsonLoader.LoadFromResources<LevelProgressionFile>("GameData/level_progression"));
        GameDataValidator.Validate(snapshot.characters, snapshot.skills, snapshot.stages, snapshot.banners, statRules);
        data.LoadValidated(snapshot.characters, snapshot.skills, snapshot.stages, snapshot.banners, statRules, levelRules);
    }

    private static Snapshot CreateSnapshot(ContentCatalogAsset asset)
    {
        asset.ValidateSchema();
        return new Snapshot
        {
            characters = Clone(asset.characters),
            skills = Clone(asset.skills),
            stages = Clone(asset.stages),
            banners = Clone(asset.banners),
        };
    }

    private static T Clone<T>(T value) where T : class
    {
        if (value == null) return null;
        string json = JsonUtility.ToJson(value);
        var clone = JsonUtility.FromJson<T>(json);
        if (clone == null) throw new InvalidOperationException($"Failed to clone Unity content section {typeof(T).Name}.");
        return clone;
    }

    private sealed class Snapshot
    {
        public CharactersFile characters;
        public SkillsFile skills;
        public StagesFile stages;
        public BannersFile banners;
    }
}
