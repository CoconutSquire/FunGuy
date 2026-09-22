using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DataAndSaveTests
{
    [Test]
    public void StageWaves_ReferenceExistingRosterOpponents()
    {
        var data = new GameData();
        data.LoadAll();

        foreach (var stage in data.Stages.Values)
        {
            Assert.NotNull(stage.waves, $"Stage {stage.id} missing wave list.");
            Assert.Greater(stage.waves.Count, 0, $"Stage {stage.id} has zero waves.");

            for (int i = 0; i < stage.waves.Count; i++)
            {
                var wave = stage.waves[i].enemies;
                Assert.NotNull(wave, $"Stage {stage.id} wave {i + 1} null.");
                Assert.Greater(wave.Count, 0, $"Stage {stage.id} wave {i + 1} empty.");
                foreach (var wu in wave)
                {
                    Assert.IsTrue(data.Characters.ContainsKey(wu.enemyId),
                        $"Stage {stage.id} wave {i + 1} references missing roster opponent {wu.enemyId}.");
                }
            }
        }
    }

    [Test]
    public void SaveModel_RoundTripsJson_WithBannerMaps()
    {
        var save = new PlayerSave
        {
            version = 2,
            gold = 123,
            spores = 456,
            accountLevel = 3,
            tutorialCompleted = false,
            tutorialStep = 2,
            units = new List<OwnedUnit>
            {
                new() { charId = "unit_a", level = 4, stars = 2, coreLevel = 1, copies = 1, xp = 25 }
            },
            bannerPity = new List<StringIntEntry>(),
            bannerFeaturedGuarantee = new List<StringBoolEntry>(),
            bannerHistory = new List<BannerHistoryEntry>()
        };
        SaveMapUtils.SetInt(save.bannerPity, "b_standard", 15);
        SaveMapUtils.SetBool(save.bannerFeaturedGuarantee, "b_event:featured", true);

        string json = JsonUtility.ToJson(save);
        var loaded = JsonUtility.FromJson<PlayerSave>(json);

        Assert.NotNull(loaded);
        Assert.AreEqual(123, loaded.gold);
        Assert.AreEqual(456, loaded.spores);
        Assert.AreEqual(2, loaded.tutorialStep);
        Assert.AreEqual(1, loaded.units.Count);
        Assert.AreEqual("unit_a", loaded.units[0].charId);
        Assert.AreEqual(15, SaveMapUtils.GetInt(loaded.bannerPity, "b_standard", 0));
        Assert.IsTrue(SaveMapUtils.GetBool(loaded.bannerFeaturedGuarantee, "b_event:featured", false));
    }
}
