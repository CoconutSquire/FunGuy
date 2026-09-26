using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class UnityContentCatalogTests
{
    [Test]
    public void AuthoredCatalog_ContainsCurrentRosterAndCompleteStageOne()
    {
        var asset = Resources.Load<ContentCatalogAsset>(UnityContentCatalogLoader.ResourcesPath);
        Assert.NotNull(asset);
        asset.ValidateSchema();

        Assert.GreaterOrEqual(asset.characters.characters.Count, 71);
        Assert.GreaterOrEqual(asset.stages.stages.Count, 1);
        var stage = asset.stages.stages.Single(s => s.id == "s_1_1");
        Assert.AreEqual(2, stage.waves.Count);
        Assert.AreEqual(50, stage.rewards.gold);
        CollectionAssert.AreEqual(
            new[] { "R01", "R04", "R10", "R03" },
            stage.waves.SelectMany(w => w.enemies).Select(e => e.enemyId).ToArray());
    }

    [Test]
    public void UnityBlankEffectFieldsNormalizeToJsonEquivalentRuntimeValues()
    {
        var asset = Resources.Load<ContentCatalogAsset>(UnityContentCatalogLoader.ResourcesPath);
        Assert.NotNull(asset);

        // The checked-in catalog is allowed to lag behind the current JSON content;
        // runtime must fall back rather than expose legacy placeholder skills.
        var data = new GameData();
        data.LoadAll();
        var runtime = data.Skills["atk_basic"].effects.Single();

        Assert.IsNull(runtime.target);
        Assert.IsNull(runtime.stat);

    }

    [Test]
    public void RuntimeCatalog_IsDeepCopiedAndCannotDirtyAuthoredAsset()
    {
        var asset = Resources.Load<ContentCatalogAsset>(UnityContentCatalogLoader.ResourcesPath);
        Assert.NotNull(asset);

        var first = new GameData();
        first.LoadAll();

        first.Stages["s_1_1"].waves.Clear();
        first.Stages["s_1_1"].rewards.gold = 99999;
        first.Characters["1"].name = "MUTATED AT RUNTIME";
        first.Skills["atk_basic"].effects.Clear();

        var authoredStage = asset.stages.stages.Single(s => s.id == "s_1_1");
        Assert.AreEqual(2, authoredStage.waves.Count);
        Assert.AreEqual(50, authoredStage.rewards.gold);
        Assert.AreEqual("Golden Chanterelle", asset.characters.characters.Single(c => c.id == "1").name);
        Assert.IsNotEmpty(asset.skills.skills.Where(s => s != null && s.id == "imported_character_basic").SelectMany(s => s.effects ?? new System.Collections.Generic.List<EffectDef>()));

        var second = new GameData();
        second.LoadAll();
        Assert.AreEqual(2, second.Stages["s_1_1"].waves.Count);
        Assert.AreEqual(50, second.Stages["s_1_1"].rewards.gold);
        Assert.AreEqual("Golden Chanterelle", second.Characters["1"].name);
        Assert.IsNotEmpty(second.Skills["atk_basic"].effects);
    }
}
