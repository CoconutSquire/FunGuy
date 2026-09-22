using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class StabilizationTests
{
    private sealed class MemoryStore : IPlayerSaveStore
    {
        public PlayerSave state = new();
        public int writes;
        public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(state));
        public void Write(PlayerSave value) { state = value; writes++; }
    }

    private static GameData Load() { var data = new GameData(); data.LoadAll(); return data; }

    [Test]
    public void ResourceCatalog_LoadsEveryWaveFromTheGarrettRoster()
    {
        var data = Load();
        Assert.AreEqual(71, data.Characters.Count);
        Assert.AreEqual(0, data.Enemies.Count);
        Assert.AreEqual(14, data.Stages.Values.Sum(s => s.waves.Count));
        Assert.True(data.Stages.Values.SelectMany(s => s.waves).SelectMany(w => w.enemies)
            .All(entry => data.Characters.ContainsKey(entry.enemyId)));
        var low = CombatUnitFactory.Create(data.Characters["R10"], 1, TeamSide.Enemy, 1, data.StatRules);
        var high = CombatUnitFactory.Create(data.Characters["R10"], 6, TeamSide.Enemy, 1, data.StatRules);
        Assert.Greater(high.maxHp, low.maxHp);
        Assert.Greater(high.atk, low.atk);
    }

    [Test]
    public void Validator_RejectsLegacySchemaAndMissingSkillAndInvalidRates()
    {
        var c = JsonLoader.LoadFromResources<CharactersFile>("GameData/characters");
        var s = JsonLoader.LoadFromResources<SkillsFile>("GameData/skills");
        var stages = JsonLoader.LoadFromResources<StagesFile>("GameData/stages");
        var b = JsonLoader.LoadFromResources<BannersFile>("GameData/banners");
        var rules = Load().StatRules;
        stages.schemaVersion = 0;
        StringAssert.Contains("schemaVersion", Assert.Throws<InvalidOperationException>(() => GameDataValidator.Validate(c, s, stages, b, rules)).Message);
        stages.schemaVersion = 1;
        c.characters[0].skills.basic = "missing";
        StringAssert.Contains("skill reference", Assert.Throws<InvalidOperationException>(() => GameDataValidator.Validate(c, s, stages, b, rules)).Message);
        c.characters[0].skills.basic = "atk_basic";
        b.banners[0].rates[0].rate = -1;
        StringAssert.Contains("rate", Assert.Throws<InvalidOperationException>(() => GameDataValidator.Validate(c, s, stages, b, rules)).Message);
    }

    private static GameData CombatData()
    {
        var data = new GameData();
        data.Skills["basic"] = new SkillDef { id = "basic", target = "EnemyFront", effects = new() { new() { type = "Damage", scale = 1 } } };
        data.Skills["ult"] = new SkillDef { id = "ult", target = "EnemyFront", energyCost = 100, cooldown = 3, effects = new() { new() { type = "Damage", scale = 10 } } };
        return data;
    }

    private static CombatUnit Unit(TeamSide side) => new()
    {
        id = side.ToString(), side = side, hp = 100, maxHp = 100, atk = 10, def = 0,
        spd = side == TeamSide.Player ? 1000 : 1, pot = 0, biome = "Kitchen",
        basicSkillId = "basic", ultSkillId = "ult", maxEnergy = 100
    };

    [Test]
    public void LethalStartOfTurnDamage_PreventsActionAndRegenResurrection()
    {
        var player = Unit(TeamSide.Player); var enemy = Unit(TeamSide.Enemy);
        player.hp = 1;
        player.statuses.Add(new() { status = "Poison", potency = .1f, remainingTurns = 2, stacks = 1 });
        player.statuses.Add(new() { status = "Regen", potency = 1, remainingTurns = 2, stacks = 1 });
        new BattleSim(CombatData(), 1).RunBattle(new() { player }, new() { enemy }, 1);
        Assert.AreEqual(0, player.hp);
        Assert.AreEqual(100, enemy.hp);
    }

    [Test]
    public void OneTurnSilence_LocksSignatureThroughActionThenExpires()
    {
        var player = Unit(TeamSide.Player); var enemy = Unit(TeamSide.Enemy);
        player.energy = 100;
        player.statuses.Add(new() { status = "Silence", remainingTurns = 1 });
        new BattleSim(CombatData(), 1).RunBattle(new() { player }, new() { enemy }, 1);
        Assert.AreEqual(90, enemy.hp);
        Assert.IsEmpty(player.statuses);
        Assert.AreEqual(100, player.energy);
    }

    [Test]
    public void RepeatedStatuses_AddTheirOwnPotencyAndKeepSeparateDurations()
    {
        var data = CombatData();
        data.Skills["dots"] = new SkillDef { id = "dots", target = "Self", effects = new()
        {
            new() { type = "ApplyStatus", status = "Poison", chance = 1, potency = .1f, duration = 1 },
            new() { type = "ApplyStatus", status = "Poison", chance = 1, potency = .2f, duration = 3 }
        }};
        var player = Unit(TeamSide.Player); var enemy = Unit(TeamSide.Enemy);
        player.basicSkillId = "dots";
        var sim = new BattleSim(data, 1);
        sim.RunBattle(new() { player }, new() { enemy }, 1);
        player.basicSkillId = "basic";
        sim.RunBattle(new() { player }, new() { enemy }, 1);
        Assert.AreEqual(70, player.hp);
        Assert.AreEqual(1, player.statuses.Count);
        Assert.AreEqual(2, player.statuses[0].remainingTurns);
    }

    [Test]
    public void Thorns_ReflectsOnceWithoutRecursiveHits()
    {
        var player = Unit(TeamSide.Player); var enemy = Unit(TeamSide.Enemy);
        player.statuses.Add(new() { status = "Thorns", potency = .5f, remainingTurns = 3 });
        enemy.statuses.Add(new() { status = "Thorns", potency = .5f, remainingTurns = 3 });
        new BattleSim(CombatData(), 1).RunBattle(new() { player }, new() { enemy }, 1);
        Assert.AreEqual(95, player.hp);
        Assert.AreEqual(90, enemy.hp);
    }

    [Test]
    public void TenPull_InsufficientCurrencyLeavesEntireSaveUnchanged()
    {
        var data = Load(); var store = new MemoryStore(); store.state.spores = 50; store.state.tutorialCompleted = true;
        var before = JsonUtility.ToJson(store.state);
        var service = new LocalSummonService(data, store, new GachaService(data, 42));
        Assert.Throws<InvalidOperationException>(() => service.Pull("b_standard", 10));
        Assert.AreEqual(before, JsonUtility.ToJson(store.state));
        Assert.AreEqual(0, store.writes);
    }

    [Test]
    public void FailedSelection_RollsBackTicketWalletPityAndInventory()
    {
        var data = Load(); var store = new MemoryStore(); store.state.tutorialTickets = 1;
        data.Characters.Clear();
        var before = JsonUtility.ToJson(store.state);
        var service = new LocalSummonService(data, store, new GachaService(data, 42));
        Assert.Throws<Exception>(() => service.Pull("b_standard", 1));
        Assert.AreEqual(before, JsonUtility.ToJson(store.state));
        Assert.AreEqual(0, store.writes);
    }

    [Test]
    public void TenPull_CommitsOnceAndChargesFullCost()
    {
        var data = Load(); var store = new MemoryStore(); store.state.spores = 100; store.state.tutorialCompleted = true;
        var result = new LocalSummonService(data, store, new GachaService(data, 42)).Pull("b_standard", 10);
        Assert.AreEqual(10, result.characterIds.Count);
        Assert.AreEqual(0, store.state.spores);
        Assert.AreEqual(1, store.writes);
        Assert.AreEqual(10, store.state.units.Sum(u => u.copies));
    }

    [Test]
    public void Campaign_FirstClearAndTutorialRewardsAreGrantedOnce()
    {
        var data = Load(); var store = new MemoryStore();
        store.state.accountLevel = 1;
        store.state.units.Add(new() { charId = "1", level = 100, stars = 1 });
        store.state.activeTeam.Add("1");
        var campaign = new LocalCampaignService(data, store);
        Assert.False(campaign.IsUnlocked("s_1_2"));
        var firstClear = campaign.Run("s_1_1");
        Assert.True(firstClear.firstClearRewardGranted);
        Assert.AreEqual(BattleOutcome.Victory, firstClear.outcome);
        Assert.AreEqual(firstClear.waveCount, firstClear.battles.Count);
        Assert.True(firstClear.battles.All(w => w.Events.Last().Kind == BattleEventKind.BattleEnded));
        Assert.AreEqual(firstClear.battles[0].InitialState.First(s => s.Side == TeamSide.Player).InstanceId,
            firstClear.battles[1].InitialState.First(s => s.Side == TeamSide.Player).InstanceId);
        Assert.True(campaign.IsUnlocked("s_1_2"));
        var gold = store.state.gold;
        Assert.False(campaign.Run("s_1_1").firstClearRewardGranted);
        Assert.AreEqual(gold, store.state.gold);
        Assert.AreEqual(1, store.state.accountLevel);
        Assert.AreEqual(5, store.state.accountXp);
        Assert.True(campaign.ClaimTutorialBattleReward());
        Assert.False(campaign.ClaimTutorialBattleReward());
        Assert.AreEqual(gold + 100, store.state.gold);
    }
}
