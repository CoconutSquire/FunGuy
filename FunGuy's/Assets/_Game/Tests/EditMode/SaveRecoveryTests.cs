using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SaveRecoveryTests
{
    private readonly string[] keys = { "FUNGI_SAVE_V2", "FUNGI_SAVE_V2_BAK", "FUNGI_SAVE_V1", "FUNGI_SAVE_V1_BAK" };
    private readonly Dictionary<string, string> backup = new();
    [SetUp] public void Setup()
    {
        foreach (var key in keys) if (PlayerPrefs.HasKey(key)) backup[key] = PlayerPrefs.GetString(key);
        SaveSystem.DeleteSave();
    }
    [TearDown] public void Cleanup()
    {
        foreach (var key in keys) PlayerPrefs.DeleteKey(key);
        foreach (var pair in backup) PlayerPrefs.SetString(pair.Key, pair.Value);
        PlayerPrefs.Save(); backup.Clear();
    }
    [Test] public void BackupKeepsPreviousSnapshotAndRecoversIt()
    {
        var save = SaveSystem.LoadOrNew();
        save.gold = 321; SaveSystem.Save(save);
        save.gold = 654; SaveSystem.Save(save);
        Assert.AreEqual(321, JsonUtility.FromJson<PlayerSave>(PlayerPrefs.GetString("FUNGI_SAVE_V2_BAK")).gold);
        PlayerPrefs.SetString("FUNGI_SAVE_V2", "not-json");
        Assert.True(SaveSystem.TryLoad(out var restored));
        Assert.AreEqual(321, restored.gold);
    }
    [Test] public void VersionTwoMigrationPreservesBalancesAndClosesTutorialClaims()
    {
        var save = new PlayerSave { version = 2, gold = 432, accountLevel = 7, tutorialStep = 7, tutorialTickets = 1 };
        PlayerPrefs.SetString("FUNGI_SAVE_V2", JsonUtility.ToJson(save));
        Assert.True(SaveSystem.TryLoad(out var migrated));
        Assert.AreEqual(5, migrated.version);
        Assert.AreEqual(432, migrated.gold);
        Assert.AreEqual(7, migrated.accountLevel);
        Assert.True(migrated.tutorialBattleRewardClaimed);
        Assert.AreEqual(0, migrated.tutorialTickets);
    }
    [Test] public void DeleteAlsoRemovesLegacyRecoveryKeys()
    {
        PlayerPrefs.SetString("FUNGI_SAVE_V1", JsonUtility.ToJson(new PlayerSave { version = 1 }));
        SaveSystem.DeleteSave();
        Assert.False(SaveSystem.HasSave());
        Assert.False(SaveSystem.TryLoad(out _));
    }

    [Test] public void VersionFourSparseBoardMigratesOnceAndKeepsFrontRearIntent()
    {
        var save = new PlayerSave { version = 4, gold = 789, tutorialCompleted = true,
            activeTeam = new() { "a", "b", "c" },
            units = new() { new() { charId = "a" }, new() { charId = "b" }, new() { charId = "c" } },
            formation = new() { new() { charId = "a", slot = 1 }, new() { charId = "b", slot = 4 }, new() { charId = "c", slot = 7 } } };
        PlayerPrefs.SetString("FUNGI_SAVE_V2", JsonUtility.ToJson(save));
        var migrated = SaveSystem.LoadOrNew();
        Assert.AreEqual(5, migrated.version);
        Assert.AreEqual(2, migrated.formation.Find(p => p.charId == "a").slot);
        Assert.AreEqual(11, migrated.formation.Find(p => p.charId == "b").slot);
        Assert.AreEqual(0, migrated.formation.Find(p => p.charId == "c").slot);
        migrated.formation.Find(p => p.charId == "c").slot = 7;
        SaveSystem.Save(migrated);
        var reloaded = SaveSystem.LoadOrNew();
        Assert.AreEqual(7, reloaded.formation.Find(p => p.charId == "c").slot);
        Assert.AreEqual(11, reloaded.formation.Find(p => p.charId == "b").slot);
        Assert.AreEqual(789, reloaded.gold);
        Assert.True(reloaded.tutorialCompleted);
    }

    [Test] public void VersionThreeFormationMigrationAndSparseRoundTripPreserveProgress()
    {
        var save = new PlayerSave { version = 3, gold = 456, tutorialCompleted = true,
            activeTeam = new() { "b", "a", "c" }, formation = null,
            units = new() { new() { charId = "a", level = 12 }, new() { charId = "b", level = 9 }, new() { charId = "c", level = 2 } } };
        PlayerPrefs.SetString("FUNGI_SAVE_V2", JsonUtility.ToJson(save));
        Assert.True(SaveSystem.TryLoad(out var migrated));
        Assert.AreEqual(5, migrated.version);
        Assert.AreEqual("b", migrated.formation[0].charId);
        Assert.AreEqual(0, migrated.formation[0].slot);
        Assert.AreEqual("c", migrated.formation[2].charId);
        migrated.formation[0].slot = 4;
        SaveSystem.Save(migrated);
        var restored = SaveSystem.LoadOrNew();
        Assert.AreEqual(456, restored.gold);
        Assert.True(restored.tutorialCompleted);
        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, restored.activeTeam);
        Assert.AreEqual(4, restored.formation.Find(p => p.charId == "b").slot);
        Assert.False(restored.formation.Exists(p => p.slot == 0));
        Assert.AreEqual(12, restored.units.Find(u => u.charId == "a").level);
    }
}
