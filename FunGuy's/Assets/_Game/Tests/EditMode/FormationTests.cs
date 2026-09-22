using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class FormationTests
{
    private sealed class Store : IPlayerSaveStore
    {
        public PlayerSave state = new();
        public int writes;
        public bool fail;
        public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(state));
        public void Write(PlayerSave save) { if (fail) throw new InvalidOperationException("Disk failure"); state = save; writes++; }
    }
    private static LocalTeamService Team(Store store)
    {
        var data = new GameData();
        foreach (string id in new[] { "a", "b", "c", "d", "e", "f" })
        {
            data.Characters[id] = new CharacterDef { id = id, rarity = 4 };
            store.state.units.Add(new OwnedUnit { charId = id, level = 1 });
        }
        return new LocalTeamService(data, store);
    }

    [Test]
    public void FullBoard_AllSpacesAvailableButSixthMemberRequiresReplacement()
    {
        var store = new Store(); var team = Team(store);
        for (int slot = 0; slot < FormationRules.SlotCount; slot++)
        {
            team.Clear();
            Assert.True(team.Place("a", slot));
            Assert.AreEqual(slot, team.GetFormation().Single().slot);
            Assert.AreEqual(slot, FormationRules.ParseSlot(FormationRules.SlotId(slot)));
        }
        team.Replace(new[] { "a", "b", "c", "d", "e" });
        int writes = store.writes;
        Assert.False(team.Place("f", 6));
        Assert.AreEqual(writes, store.writes);
        Assert.True(team.Place("f", 8));
        Assert.AreEqual(5, team.GetFormation().Count);
        Assert.False(store.state.activeTeam.Contains("c"));
        Assert.AreEqual(6, store.state.units.Count);
        Assert.True(team.SwapSlots(8, 6));
        Assert.AreEqual("f", team.GetFormation().Single(p => p.slot == 6).charId);
    }

    [Test]
    public void MiddleDepth_IsIndependentAndFallbackStopsAtNearestOccupiedDepth()
    {
        var front = Unit("front", TeamSide.Enemy, 0); front.hp = 0;
        var middle = Unit("middle", TeamSide.Enemy, 0); middle.formationSlot = 6;
        var rear = Unit("rear", TeamSide.Enemy, 4);
        CollectionAssert.AreEqual(new[] { middle }, FormationRules.LivingRow(new() { rear, front, middle }, true, true));
        CollectionAssert.AreEqual(new[] { rear }, FormationRules.LivingRow(new() { middle, rear }, false, true));
        CollectionAssert.AreEqual(new[] { middle }, FormationRules.LivingSameRow(new() { middle, rear }, 7));
        rear.hp = 0;
        CollectionAssert.AreEqual(new[] { middle }, FormationRules.LivingRow(new() { middle, rear }, false, true));
    }

    [Test]
    public void Placement_SwapsMovesReplacesAndKeepsHolesWithoutLosingOwnedUnits()
    {
        var store = new Store(); var team = Team(store);
        team.Replace(new[] { "a", "b", "c", "d", "e" });
        Assert.True(team.Place("a", 11));
        Assert.AreEqual("e", team.GetFormation().Single(p => p.slot == 0).charId);
        Assert.True(team.Place("f", 2));
        Assert.False(store.state.activeTeam.Contains("b"));
        Assert.AreEqual(6, store.state.units.Count);
        Assert.True(team.Remove("c"));
        Assert.False(team.GetFormation().Any(p => p.slot == 8));
        Assert.True(team.SwapSlots(11, 8));
        Assert.AreEqual("a", team.GetFormation().Single(p => p.slot == 8).charId);
        Assert.False(team.GetFormation().Any(p => p.slot == 11));
        Assert.True(team.Add("b"));
        Assert.AreEqual("b", team.GetFormation().Single(p => p.slot == 11).charId);
        var copy = team.GetFormation(); copy[0].slot = 99;
        Assert.False(team.GetFormation().Any(p => p.slot == 99));
    }

    [Test]
    public void InvalidNoOpAndFailedOperations_DoNotPublishPartialPlacement()
    {
        var store = new Store(); var team = Team(store);
        team.Place("a", 11);
        string original = JsonUtility.ToJson(store.state);
        Assert.False(team.Place("a", 11));
        Assert.False(team.Place("missing", 1));
        Assert.False(team.SwapSlots(0, 1));
        Assert.False(team.SwapSlots(11, 11));
        Assert.Throws<ArgumentOutOfRangeException>(() => team.Place("b", 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => team.SwapSlots(-1, 2));
        Assert.AreEqual(1, store.writes);
        store.fail = true;
        Assert.Throws<InvalidOperationException>(() => team.SwapSlots(11, 0));
        Assert.Throws<InvalidOperationException>(() => team.Place("b", 11));
        Assert.AreEqual(original, JsonUtility.ToJson(store.state));
    }

    [Test]
    public void LegacyAndDamagedAssignments_RecoverDeterministically()
    {
        var save = new PlayerSave { activeTeam = new() { "a", "b", "c" }, formation = null };
        CollectionAssert.AreEqual(new[] { 0, 2, 8 }, FormationRules.Resolve(save).Select(p => p.slot));
        save.formation = new() { new() { charId = "a", slot = 4 }, new() { charId = "b", slot = 4 },
            new() { charId = "c", slot = 99 }, new() { charId = "outsider", slot = 1 }, null };
        var recovered = FormationRules.Resolve(save);
        Assert.AreEqual(4, recovered.Single(p => p.charId == "a").slot);
        Assert.AreEqual(0, recovered.Single(p => p.charId == "b").slot);
        Assert.AreEqual(2, recovered.Single(p => p.charId == "c").slot);
        Assert.AreEqual(3, recovered.Count);
    }

    private static CombatUnit Unit(string id, TeamSide side, int slot) => new() {
        id = id, side = side, formationSlot = slot == -1 ? -1 : FormationRules.MigrateLegacySlot(slot), hp = 100, maxHp = 100, atk = 10, def = 0, pot = 0,
        spd = side == TeamSide.Player ? 1000 : 1, biome = "Kitchen", basicSkillId = "hit", ultSkillId = "hit"
    };
    private static GameData Data(string target) => new() { Skills = new() {
        ["hit"] = new SkillDef { id = "hit", target = target, effects = new() { new() { type = "Damage", scale = 1 } } }
    } };

    [TestCase("EnemyFront", 100, 90, 100)]
    [TestCase("EnemyBack", 90, 100, 100)]
    [TestCase("EnemyFrontRow", 100, 90, 90)]
    [TestCase("EnemyBackRow", 90, 100, 100)]
    public void Targeting_UsesPositionsRatherThanCollectionOrder(string rule, int backHp, int leftHp, int rightHp)
    {
        var back = Unit("back", TeamSide.Enemy, 3); var left = Unit("left", TeamSide.Enemy, 0); var right = Unit("right", TeamSide.Enemy, 1);
        new BattleSim(Data(rule), 1).RunBattle(new() { Unit("actor", TeamSide.Player, 4) }, new() { back, right, left }, 1);
        CollectionAssert.AreEqual(new[] { backHp, leftHp, rightHp }, new[] { back.hp, left.hp, right.hp });
    }

    [Test]
    public void DeadFrontlineFallsBackToBacklineWithoutMovingSurvivors()
    {
        var dead = Unit("dead", TeamSide.Enemy, 0); dead.hp = 0;
        var back = Unit("back", TeamSide.Enemy, 4);
        new BattleSim(Data("EnemyFront"), 1).RunBattle(new() { Unit("actor", TeamSide.Player, 0) }, new() { dead, back }, 1);
        Assert.AreEqual(90, back.hp); Assert.AreEqual(11, back.formationSlot);
    }

    [Test]
    public void SwappingFrontAndBackChangesWhoSurvivesTheAttack()
    {
        var actor = Unit("attacker", TeamSide.Player, 0); actor.atk = 100;
        var tank = Unit("tank", TeamSide.Enemy, 0); tank.hp = tank.maxHp = 500;
        var fragile = Unit("fragile", TeamSide.Enemy, 2); fragile.hp = 50;
        new BattleSim(Data("EnemyFront"), 1).RunBattle(new() { actor }, new() { fragile, tank }, 1);
        Assert.AreEqual(400, tank.hp); Assert.AreEqual(50, fragile.hp);
        tank.formationSlot = 8; fragile.formationSlot = 0;
        new BattleSim(Data("EnemyFront"), 1).RunBattle(new() { actor }, new() { fragile, tank }, 1);
        Assert.AreEqual(0, fragile.hp); Assert.AreEqual(400, tank.hp);
    }

    [Test]
    public void AllyRowAndInvalidBattleSlotsRespectBoardBoundaries()
    {
        var actor = Unit("actor", TeamSide.Player, 2); var sameRow = Unit("ally", TeamSide.Player, 4); var front = Unit("front", TeamSide.Player, 0);
        sameRow.spd = front.spd = 1;
        var data = Data("AllyRow"); data.Skills["hit"].effects[0] = new EffectDef { type = "Shield", scale = .1f };
        new BattleSim(data, 1).RunBattle(new() { actor, sameRow, front }, new() { Unit("enemy", TeamSide.Enemy, 0) }, 1);
        Assert.AreEqual(10, actor.shield); Assert.AreEqual(10, sameRow.shield); Assert.AreEqual(0, front.shield);
        Assert.Throws<ArgumentException>(() => FormationRules.AssignBattleSlots(new() { Unit("a", TeamSide.Player, 0), Unit("b", TeamSide.Player, 0) }));
        var fixedBack = Unit("a", TeamSide.Enemy, 4); var unspecified = Unit("b", TeamSide.Enemy, -1);
        FormationRules.AssignBattleSlots(new() { fixedBack, unspecified });
        Assert.AreEqual(0, unspecified.formationSlot); Assert.AreEqual(11, fixedBack.formationSlot);
    }

    [Test]
    public void EnemyContent_ReservesExplicitSlotsAndRejectsDuplicateAssignments()
    {
        var data = new GameData(); data.LoadAll();
        var entry = JsonUtility.FromJson<WaveUnit>("{\"enemyId\":\"R10\",\"level\":2,\"slotId\":\"back_right\"}");
        var unit = CombatUnitFactory.Create(data.Characters[entry.enemyId], entry, TeamSide.Enemy, data.StatRules);
        Assert.AreEqual(11, unit.formationSlot); Assert.AreEqual("R10", unit.id); Assert.Greater(unit.maxHp, 140);
        var c = JsonLoader.LoadFromResources<CharactersFile>("GameData/characters");
        var s = JsonLoader.LoadFromResources<SkillsFile>("GameData/skills");
        var stages = JsonLoader.LoadFromResources<StagesFile>("GameData/stages");
        var b = JsonLoader.LoadFromResources<BannersFile>("GameData/banners");
        stages.stages[0].waves[0].enemies = new() { entry, new() { enemyId = entry.enemyId, level = 1, slotId = "back_right" } };
        Assert.Throws<InvalidOperationException>(() => GameDataValidator.Validate(c, s, stages, b, data.StatRules));
        stages.stages[0].waves[0].enemies[1].slotId = null;
        Assert.DoesNotThrow(() => GameDataValidator.Validate(c, s, stages, b, data.StatRules));
    }
}
