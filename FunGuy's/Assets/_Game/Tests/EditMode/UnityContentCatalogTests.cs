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

        Assert.AreEqual(71, asset.characters.characters.Count);
        Assert.AreEqual(7, asset.stages.stages.Count);
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

        var authored = asset.skills.skills.Single(s => s.id == "imported_character_basic").effects.Single();
        Assert.AreEqual("", authored.target, "Unity asset should demonstrate the empty-string serialization case.");

        var data = new GameData();
        data.LoadAll();
        var runtime = data.Skills["imported_character_basic"].effects.Single();

        Assert.IsNull(runtime.target);
        Assert.AreEqual("ATK", runtime.stat);
        Assert.AreEqual("", authored.target, "Runtime normalization must not mutate the authored asset.");
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
        first.Skills["imported_character_basic"].effects.Clear();

        var authoredStage = asset.stages.stages.Single(s => s.id == "s_1_1");
        Assert.AreEqual(2, authoredStage.waves.Count);
        Assert.AreEqual(50, authoredStage.rewards.gold);
        Assert.AreEqual("Golden Chanterelle", asset.characters.characters.Single(c => c.id == "1").name);
        Assert.IsNotEmpty(asset.skills.skills.Single(s => s.id == "imported_character_basic").effects);

        var second = new GameData();
        second.LoadAll();
        Assert.AreEqual(2, second.Stages["s_1_1"].waves.Count);
        Assert.AreEqual(50, second.Stages["s_1_1"].rewards.gold);
        Assert.AreEqual("Golden Chanterelle", second.Characters["1"].name);
        Assert.IsNotEmpty(second.Skills["imported_character_basic"].effects);
    }
}
