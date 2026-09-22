using System;
using System.Collections.Generic;
using System.Linq;
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

    public static void LoadInto(GameData data, ContentCatalogAsset asset)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (asset == null) throw new ArgumentNullException(nameof(asset));
        asset.ValidateSchema();

        var statRules = new StatRulesCatalog(JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules"));
        var levelRules = new LevelProgressionRules(JsonLoader.LoadFromResources<LevelProgressionFile>("GameData/level_progression"));
        GameDataValidator.Validate(asset.characters, asset.skills, asset.stages, asset.banners, statRules);

        data.LoadValidated(asset.characters, asset.skills, asset.stages, asset.banners, statRules, levelRules);
    }
}
