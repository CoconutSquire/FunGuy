using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class UpgradeServiceTests
{
    private sealed class Store : IPlayerSaveStore
    {
        public PlayerSave state = new();
        public int writes;
        public bool fail;
        public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(state));
        public void Write(PlayerSave save)
        {
            if (fail) throw new InvalidOperationException("Storage unavailable");
            state = JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(save)); writes++;
        }
    }
    private GameData data;
    private Store store;
    private LocalUpgradeService service;
    private const string Id = "1";
    [SetUp] public void Setup()
    {
        data = new GameData(); data.LoadAll(); store = new Store(); store.state.gold = 250;
        store.state.units.Add(new OwnedUnit { charId = Id, level = 1, stars = 1, copies = 3, xp = 17, coreLevel = 1 });
        store.state.activeTeam.Add(Id); store.state.formation.Add(new FormationPlacement { charId = Id, slot = 11 });
        service = new LocalUpgradeService(data, store);
    }
    [Test] public void LevelUp_UsesSharedStatsAndCommitsOnlyLevelAndGold()
    {
        var first = service.Preview(Id); string before = JsonUtility.ToJson(store.state);
        Assert.AreEqual(25, first.GoldCost); Assert.AreEqual(100, first.LevelCap);
        Assert.AreEqual(before, JsonUtility.ToJson(store.state)); Assert.AreEqual(0, store.writes);
        var result = service.LevelUp(Id, first.Level, first.RulesVersion);
        Assert.AreEqual(2, result.Level); Assert.AreEqual(225, result.Gold); Assert.AreEqual(35, result.GoldCost);
        Assert.AreEqual(first.Next.HP, result.Current.HP);
        var actual = CombatUnitFactory.Create(data.Characters[Id], 2, TeamSide.Player, 1, data.StatRules);
        Assert.AreEqual(actual.maxHp, result.Current.HP); Assert.AreEqual(actual.atk, result.Current.ATK);
        Assert.AreEqual(actual.def, result.Current.DEF); Assert.AreEqual(actual.spd, result.Current.SPD); Assert.AreEqual(actual.pot, result.Current.POT);
        var expected = JsonUtility.FromJson<PlayerSave>(before); expected.gold = 225; expected.units[0].level = 2;
        Assert.AreEqual(JsonUtility.ToJson(expected), JsonUtility.ToJson(store.state)); Assert.AreEqual(1, store.writes);
        Assert.AreEqual(1, first.Level); // A displayed quote stays detached.
    }
    [Test] public void AscensionMilestones_RequireSpores()
    {
        store.state.gold = 10000; store.state.spores = 100; store.state.units[0].level = 59;
        var preview = service.Preview(Id);
        Assert.AreEqual(605, preview.GoldCost); Assert.AreEqual(100, preview.SporeCost); Assert.True(preview.RequiresAscension);
        Assert.True(preview.CanAfford);
        var result = service.LevelUp(Id, 59, preview.RulesVersion);
        Assert.AreEqual(60, result.Level); Assert.AreEqual(9395, result.Gold); Assert.AreEqual(0, result.Spores);
    }
    [Test] public void DuplicateOrStaleRequest_CannotBuyAnAdditionalLevel()
    {
        var first = service.Preview(Id); service.LevelUp(Id, first.Level, first.RulesVersion);
        Assert.Throws<InvalidOperationException>(() => service.LevelUp(Id, first.Level, first.RulesVersion));
        Assert.Throws<InvalidOperationException>(() => service.LevelUp(Id, 2, "old-rules"));
        Assert.AreEqual(2, store.state.units[0].level); Assert.AreEqual(225, store.state.gold); Assert.AreEqual(1, store.writes);
    }
    [Test] public void Commit_RereadsWalletAndPreservesInterveningRewardsAndFormation()
    {
        var first = service.Preview(Id); store.state.gold += 95; store.state.clearedStages.Add("s_1_4");
        store.state.formation[0].slot = 7;
        service.LevelUp(Id, first.Level, first.RulesVersion);
        Assert.AreEqual(320, store.state.gold); Assert.AreEqual(7, store.state.formation[0].slot);
        Assert.Contains("s_1_4", store.state.clearedStages);
    }
    [Test] public void UnaffordableAndFailedWrite_LeaveWholeSaveUntouchedAndAllowRetry()
    {
        var first = service.Preview(Id); store.state.gold = 24; string before = JsonUtility.ToJson(store.state);
        Assert.False(service.Preview(Id).CanAfford);
        Assert.Throws<InvalidOperationException>(() => service.LevelUp(Id, first.Level, first.RulesVersion));
        Assert.AreEqual(before, JsonUtility.ToJson(store.state));
        store.state.gold = 25; store.fail = true; before = JsonUtility.ToJson(store.state);
        Assert.Throws<InvalidOperationException>(() => service.LevelUp(Id, 1, first.RulesVersion));
        Assert.AreEqual(before, JsonUtility.ToJson(store.state)); Assert.AreEqual(0, store.writes);
        store.fail = false; service.LevelUp(Id, 1, first.RulesVersion);
        Assert.AreEqual(0, store.state.gold); Assert.AreEqual(1, store.writes);
    }
    [Test] public void Cap_DoesNotSpendOrDowngradeOlderSaves()
    {
        foreach (int level in new[] { 100, 101 })
        {
            store.state.units[0].level = level; var preview = service.Preview(Id);
            Assert.True(preview.AtCap); Assert.IsNull(preview.Next); Assert.False(preview.CanAfford);
            Assert.Throws<InvalidOperationException>(() => service.LevelUp(Id, level, preview.RulesVersion));
            Assert.AreEqual(level, store.state.units[0].level); Assert.AreEqual(250, store.state.gold);
        }
        Assert.AreEqual(0, store.writes);
    }
    [Test] public void MissingDuplicateAndInvalidOwnedState_CannotSpend()
    {
        Assert.Throws<InvalidOperationException>(() => service.Preview("missing"));
        Assert.Throws<InvalidOperationException>(() => service.Preview("2"));
        store.state.units.Add(new OwnedUnit { charId = Id, level = 1, stars = 1 });
        Assert.Throws<InvalidOperationException>(() => service.LevelUp(Id, 1, LevelProgressionRules.Version));
        store.state.units.RemoveAt(1); store.state.units[0].stars = 7;
        Assert.Throws<InvalidOperationException>(() => service.Preview(Id));
        store.state.units[0].stars = 1; store.state.units[0].level = 0;
        Assert.Throws<InvalidOperationException>(() => service.Preview(Id)); Assert.AreEqual(0, store.writes);
    }
    [Test] public void CanonicalProfile_UsesEvolutionAndClassGrowthWithoutChangingStars()
    {
        var profile = data.StatRules.Character("sheet_01");
        data.Characters[Id] = new CharacterDef { id = Id, statProfileId = profile.id, skills = data.Characters[Id].skills };
        store.state.units[0].stars = 2;
        var preview = service.Preview(Id); var expected = data.StatRules.CalculateCharacter(profile.id, 2, 2);
        Assert.AreEqual(expected.hp, preview.Next.HP); Assert.AreEqual(expected.spd, preview.Next.SPD);
        service.LevelUp(Id, 1, preview.RulesVersion); Assert.AreEqual(2, store.state.units[0].stars);
    }
    [Test] public void Catalog_RejectsMissingDuplicateInvalidAndUnsupportedCosts_AndCopiesInputs()
    {
        LevelProgressionFile Valid() => new() { schemaVersion = 2, rulesVersion = LevelProgressionRules.Version,
            levelCap = 3, costs = new List<LevelCostDef> { new() { fromLevel = 1, gold = 25, spores = 0 }, new() { fromLevel = 2, gold = 35, spores = 0 } } };
        var file = Valid(); var rules = new LevelProgressionRules(file); file.costs[0].gold = 1;
        Assert.AreEqual(25, rules.Cost(1)); Assert.AreEqual(0, rules.SporeCost(1)); Assert.Throws<ArgumentOutOfRangeException>(() => rules.Cost(3));
        file = Valid(); file.costs.RemoveAt(0); Assert.Throws<InvalidOperationException>(() => new LevelProgressionRules(file));
        file = Valid(); file.costs[1].fromLevel = 1; Assert.Throws<InvalidOperationException>(() => new LevelProgressionRules(file));
        file = Valid(); file.costs[0].gold = -1; Assert.Throws<InvalidOperationException>(() => new LevelProgressionRules(file));
        file = Valid(); file.costs[0].spores = -1; Assert.Throws<InvalidOperationException>(() => new LevelProgressionRules(file));
        file = Valid(); file.rulesVersion = "unknown"; Assert.Throws<InvalidOperationException>(() => new LevelProgressionRules(file));
    }
}
