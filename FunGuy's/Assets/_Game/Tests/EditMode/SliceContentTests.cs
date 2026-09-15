using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class SliceContentTests
{
    private sealed class Store : IPlayerSaveStore
    {
        public PlayerSave state = new();
        public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(state));
        public void Write(PlayerSave value) => state = value;
    }
    private static readonly string[] Starters = { "c_barkrot_thane", "c_puffmage_orbi", "c_mosswhisper_luma" };
    private static Store NewAccount()
    {
        var store = new Store(); store.state.gold = 100;
        store.state.units = Starters.Select(id => new OwnedUnit { charId = id, level = 3, stars = 1, copies = 1 }).ToList();
        store.state.activeTeam = Starters.ToList();
        store.state.formation = Starters.Select((id, n) => new FormationPlacement { charId = id, slot = new[] { 1, 9, 11 }[n] }).ToList();
        return store;
    }
    private static CampaignResult Drain(CampaignSession session)
    {
        do { while (session.Battle.Outcome == BattleOutcome.Running) session.Step(); } while (session.AdvanceWave());
        return session.Complete();
    }

    [Test] public void Roster_HasTenDistinctKitsAcrossAllClassesAndBiomes_WithSourceGrowth()
    {
        var data = new GameData(); data.LoadAll();
        Assert.AreEqual(10, data.Characters.Count);
        Assert.AreEqual(6, data.Characters.Values.Select(c => c.classArchetype).Distinct().Count());
        Assert.AreEqual(5, data.Characters.Values.Where(c => c.biome != "Biome-less").Select(c => c.biome).Distinct().Count());
        Assert.AreEqual(10, data.Characters.Values.Select(c => c.skills.ult).Distinct().Count());
        foreach (var c in data.Characters.Values)
        {
            Assert.AreEqual("class-growth-v1", c.statModel); Assert.AreEqual("slice-roster-v1", c.kitVersion);
            Assert.IsNotEmpty(c.passives); Assert.IsNotEmpty(data.Skills[c.skills.basic].description);
            Assert.IsNotEmpty(data.Skills[c.skills.ult].description);
            Assert.True(c.passives.All(p => !string.IsNullOrEmpty(p.description)));
            Assert.AreEqual(c.rarityTier == "R" ? 500 : c.biome == "Kitchen" ? 575 : 600, c.bst);
            if (c.rarityTier == "R") Assert.AreEqual("Biome-less", c.biome);
        }
        var orbi = CombatUnitFactory.Create(data.Characters[Starters[1]], 3, TeamSide.Player, 1, data.StatRules);
        Assert.AreEqual(126, orbi.maxHp); Assert.AreEqual(130, orbi.pot); Assert.AreEqual(125, orbi.spd);
        var grown = CombatUnitFactory.Create(data.Characters[Starters[1]], 3, TeamSide.Player, 2, data.StatRules);
        Assert.AreEqual(151, grown.maxHp); Assert.AreEqual(140, grown.spd);
    }

    [Test] public void StarterCampaign_CanReachBossThroughEarnedUpgradesWithoutSummonLuck()
    {
        var data = new GameData(); data.LoadAll();
        var lines = new List<string> { "seed,stage,level,goldBefore,outcome,actions" };
        var failures = new List<string>();
        for (int seed = 0; seed < 20; seed++)
        {
            var store = NewAccount(); var campaign = new LocalCampaignService(data, store); var upgrade = new LocalUpgradeService(data, store);
            foreach (var stage in data.Stages.Values.OrderBy(s => s.order))
            {
                int target = stage.order + 2;
                foreach (string id in Starters)
                {
                    var quote = upgrade.Preview(id);
                    while (quote.Level < target && quote.CanAfford)
                        quote = upgrade.LevelUp(id, quote.Level, quote.RulesVersion);
                }
                int gold = store.state.gold;
                var result = Drain(campaign.Begin(stage.id, seed, auto: true));
                int actions = result.battles.Sum(w => w.Events.Count(e => e.Kind == BattleEventKind.ActionCompleted));
                lines.Add($"{seed},{stage.id},{store.state.units.Min(u => u.level)},{gold},{result.outcome},{actions}");
                if (!result.won) { failures.Add($"Seed {seed}: {stage.id} at level {store.state.units.Min(u => u.level)} = {result.outcome}"); break; }
                Assert.True(result.firstClearRewardGranted); Assert.AreEqual("kitchen-slice-v1", result.contentVersion);
                campaign.ClaimTutorialBattleReward();
            }
        }
        string output = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../..", "artifacts/step4-content"));
        System.IO.Directory.CreateDirectory(output); System.IO.File.WriteAllLines(System.IO.Path.Combine(output, "campaign-balance.csv"), lines);
        Assert.IsEmpty(failures, string.Join("\n", failures));
    }

    [Test] public void EveryAuthoredKit_ExecutesItsBasicSignatureAndPassive()
    {
        var data = new GameData(); data.LoadAll();
        foreach (var definition in data.Characters.Values)
        {
            var player = CombatUnitFactory.Create(definition, 20, TeamSide.Player, 1, data.StatRules);
            var dummy = CombatUnitFactory.Create(data.Enemies["e_sporeling"], 1, TeamSide.Enemy);
            dummy.maxHp = dummy.hp = 50000; dummy.atk = 1; dummy.pot = 1; dummy.ultSkillId = dummy.basicSkillId;
            var session = new BattleSession(data, new() { player }, new() { dummy }, seed: 17, auto: true, maxActions: 80);
            while (session.Outcome == BattleOutcome.Running) session.Step();
            Assert.True(session.Events.Any(e => e.Kind == BattleEventKind.SkillUsed && e.Detail == definition.skills.basic), definition.id + " basic");
            Assert.True(session.Events.Any(e => e.Kind == BattleEventKind.SkillUsed && e.Detail == definition.skills.ult), definition.id + " signature");
            Assert.True(session.Events.Any(e => e.Kind == BattleEventKind.PassiveTriggered && e.Detail == definition.passives[0].id), definition.id + " passive");
        }
    }

    [Test] public void Boss_PunishesAnUnpreparedStarterTeam()
    {
        var data = new GameData(); data.LoadAll(); int losses = 0;
        for (int seed = 0; seed < 20; seed++)
        {
            var store = NewAccount(); store.state.clearedStages.AddRange(data.Stages.Keys.Where(id => id != "s_1_7"));
            var campaign = new LocalCampaignService(data, store);
            var result = Drain(campaign.Begin("s_1_7", seed, auto: true));
            if (!result.won) losses++;
        }
        TestContext.WriteLine($"Unupgraded starter team boss losses: {losses}/20");
        Assert.Greater(losses, 0, "Boss should create a reason to prepare or upgrade.");
    }

    [Test] public void AuthoredRosterUpgrade_PreservesExistingSaveAndFirstClearHistory()
    {
        var data = new GameData(); data.LoadAll(); var store = NewAccount();
        store.state.clearedStages.AddRange(new[] { "s_1_1", "s_1_2" }); store.state.spores = 48;
        store.state.units[0].copies = 7; store.state.units[0].xp = 42; store.state.units[0].stars = 2;
        string before = JsonUtility.ToJson(store.state);
        var campaign = new LocalCampaignService(data, store);
        Assert.True(campaign.IsUnlocked("s_1_3")); Assert.False(campaign.IsUnlocked("s_1_4"));
        Assert.AreEqual(before, JsonUtility.ToJson(store.state));
        var upgrade = new LocalUpgradeService(data, store); var quote = upgrade.Preview(Starters[0]);
        upgrade.LevelUp(Starters[0], quote.Level, quote.RulesVersion);
        Assert.AreEqual(7, store.state.units[0].copies); Assert.AreEqual(42, store.state.units[0].xp);
        Assert.AreEqual(2, store.state.units[0].stars); Assert.AreEqual(48, store.state.spores);
        Assert.AreEqual(2, store.state.clearedStages.Count); Assert.AreEqual(1, store.state.formation[0].slot);
    }
}
