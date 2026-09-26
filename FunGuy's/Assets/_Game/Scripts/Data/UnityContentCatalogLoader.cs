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

        // The catalog is an authored cache of the JSON content. If an older catalog
        // is still present, do not let it silently replace the current 71-character
        // roster with legacy imported/placeholder skill references. A complete
        // catalog remains authoritative; an incomplete/stale one falls back to the
        // normal JSON path in GameData.LoadAll().
        return IsCurrentCompleteCatalog(asset);
    }

    private static bool IsCurrentCompleteCatalog(ContentCatalogAsset asset)
    {
        var characters = asset.characters?.characters;
        var skills = asset.skills?.skills;
        if (characters == null || skills == null || characters.Count < 71) return false;

        var skillIds = new System.Collections.Generic.HashSet<string>(
            skills.Where(s => s != null && !string.IsNullOrWhiteSpace(s.id)).Select(s => s.id),
            StringComparer.Ordinal);

        foreach (var character in characters)
        {
            if (character == null || character.skills == null) return false;
            if (!skillIds.Contains(character.skills.basic)) return false;

            var signature = string.IsNullOrWhiteSpace(character.skills.signature)
                ? character.skills.ult
                : character.skills.signature;
            if (!skillIds.Contains(signature)) return false;

            if ((character.rarityTier == "SR" || character.rarityTier == "UR") &&
                (string.IsNullOrWhiteSpace(character.skills.ultimate) || !skillIds.Contains(character.skills.ultimate)))
                return false;
        }

        // Explicitly reject the old imported placeholder skill that previously made
        // the runtime display generic signatures instead of authored kits.
        if (skills.Any(s => s != null && s.id == "imported_character_signature" &&
                            string.Equals(s.name, "Signature Placeholder", StringComparison.OrdinalIgnoreCase)))
            return false;

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
