using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class CombatFoundationTests
{
    private static CombatUnit Unit(TeamSide side, int slot = 0) => new() {
        id = "unit", name = "Unit", side = side, formationSlot = slot, maxHp = 1000, hp = 1000,
        atk = 100, pot = 0, def = 0, spd = side == TeamSide.Player ? 1000 : 1, biome = "Biome-less",
        basicSkillId = "basic", ultSkillId = "signature"
    };
    private static GameData Data(params EffectDef[] effects) => new() { Skills = new() {
        ["basic"] = new() { id = "basic", target = "EnemyFront", effects = effects.Length == 0 ? new() { new() { type = "Damage", scale = 1 } } : effects.ToList() },
        ["signature"] = new() { id = "signature", target = "Self", energyCost = 100, cooldown = 3,
            effects = new() { new() { type = "Shield", scale = .2f } } }
    }};
    private static StatusInstance Status(string name, float potency = 0, int duration = 3) =>
        new() { status = name, potency = potency, remainingTurns = duration, stacks = 1 };
    private static BattleFighterState State(BattleSession s, TeamSide side, int slot = 0) => s.GetState().Single(u => u.Side == side && u.Slot == slot);
    private static EffectDef Apply(string name, float potency = 0, int duration = 3) =>
        new() { type = "ApplyStatus", status = name, chance = 1, potency = potency, duration = duration };

    [Test] public void PoisonUsesMaxHpBurnUsesFlatDamageAndBleedRequiresAnAction() {
        var p = Unit(TeamSide.Player); var e = Unit(TeamSide.Enemy);
        p.statuses.AddRange(new[] { Status("Poison", .1f), Status("Burn", 7), Status("Bleed", 13), Status("Stun", duration: 1) });
        var s = new BattleSession(Data(), new() { p }, new() { e });
        s.Step(); Assert.AreEqual(893, State(s, TeamSide.Player).Hp); Assert.AreEqual(1000, State(s, TeamSide.Enemy).Hp);
        s.Step(); Assert.AreEqual(773, State(s, TeamSide.Player).Hp); Assert.AreEqual(900, State(s, TeamSide.Enemy).Hp);
        var burn = Unit(TeamSide.Enemy); burn.def = 3000; burn.statuses.Add(Status("Burn", 7));
        s = new BattleSession(Data(), new() { Unit(TeamSide.Player) }, new() { burn }); s.Step();
        Assert.AreEqual(947, State(s, TeamSide.Enemy).Hp); // DEF 2700, round(100 * 3000/5700).
    }

    [Test] public void LethalBleedPreventsEffectsAndImmortalityIsConsumedOnce() {
        var p = Unit(TeamSide.Player); p.hp = 10; p.statuses.Add(Status("Bleed", 20));
        var s = new BattleSession(Data(), new() { p }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(BattleOutcome.Defeat, s.Outcome); Assert.AreEqual(1000, State(s, TeamSide.Enemy).Hp);
        var e = Unit(TeamSide.Enemy); e.hp = 10; e.statuses.Add(Status("Immortality"));
        s = new BattleSession(Data(), new() { Unit(TeamSide.Player) }, new() { e }); s.Step();
        Assert.AreEqual(1, State(s, TeamSide.Enemy).Hp); Assert.IsEmpty(State(s, TeamSide.Enemy).Statuses);
        s.Step(); Assert.AreEqual(BattleOutcome.Victory, s.Outcome);
    }

    [Test] public void TauntStealthAndVantageHaveExplicitSingleTargetPrecedence() {
        var p = Unit(TeamSide.Player); var front = Unit(TeamSide.Enemy); var back = Unit(TeamSide.Enemy, 11);
        front.statuses.Add(Status("Stealth")); back.statuses.Add(Status("Taunt"));
        var s = new BattleSession(Data(), new() { p }, new() { front, back }); s.Step();
        Assert.AreEqual(1000, State(s, TeamSide.Enemy).Hp); Assert.AreEqual(900, State(s, TeamSide.Enemy, 11).Hp);
        back.statuses.Add(Status("Stealth"));
        s = new BattleSession(Data(), new() { p }, new() { front, back }); s.Step();
        Assert.False(s.Events.Any(x => x.Kind == BattleEventKind.Damage));
        p.statuses.Add(Status("Vantage"));
        s = new BattleSession(Data(), new() { p }, new() { front, back }); s.Step();
        Assert.AreEqual(900, State(s, TeamSide.Enemy, 11).Hp);
    }

    [Test] public void CoverInterceptsOncePerActionButNeverAoEAndUsesGuardDefense() {
        var p = Unit(TeamSide.Player); var front = Unit(TeamSide.Enemy); var guard = Unit(TeamSide.Enemy, 11);
        guard.def = 3000; guard.statuses.Add(Status("Cover", 1, -1));
        var data = Data(new() { type = "Damage", scale = 1 }, new() { type = "Damage", scale = 1 });
        var s = new BattleSession(data, new() { p }, new() { front, guard }); s.Step();
        Assert.AreEqual(950, State(s, TeamSide.Enemy, 11).Hp); Assert.AreEqual(900, State(s, TeamSide.Enemy).Hp);
        Assert.AreEqual(1, s.Events.Count(x => x.Kind == BattleEventKind.Redirected));
        data.Skills["basic"].target = "AllEnemies";
        s = new BattleSession(data, new() { p }, new() { front, guard }); s.Step();
        Assert.AreEqual(900, State(s, TeamSide.Enemy, 11).Hp); Assert.AreEqual(800, State(s, TeamSide.Enemy).Hp);
        Assert.False(s.Events.Any(x => x.Kind == BattleEventKind.Redirected));
    }

    [Test] public void CloakConsumesOneHitAndRootPreventsDodgingAndMovement() {
        var e = Unit(TeamSide.Enemy); e.statuses.Add(Status("Cloak"));
        var s = new BattleSession(Data(), new() { Unit(TeamSide.Player) }, new() { e });
        s.Step(); Assert.AreEqual(1000, State(s, TeamSide.Enemy).Hp); Assert.IsEmpty(State(s, TeamSide.Enemy).Statuses);
        s.Step(); Assert.AreEqual(900, State(s, TeamSide.Enemy).Hp);
        e.statuses.Add(Status("Root"));
        s = new BattleSession(Data(new() { type = "Damage", scale = 1 }, new() { type = "Move", slot = 11 }), new() { Unit(TeamSide.Player) }, new() { e });
        s.Step(); Assert.AreEqual(900, State(s, TeamSide.Enemy).Hp);
        Assert.True(s.Events.Any(x => x.Kind == BattleEventKind.EffectBlocked && x.Detail == "Root"));
    }

    [Test] public void MovementPreservesInstanceIdentityRejectsOccupiedCellsAndChangesRowTargeting() {
        var data = Data(new EffectDef { type = "Move", slot = 11 });
        var s = new BattleSession(data, new() { Unit(TeamSide.Player) }, new() { Unit(TeamSide.Enemy) });
        string id = State(s, TeamSide.Enemy).InstanceId; s.Step();
        Assert.AreEqual(id, State(s, TeamSide.Enemy, 11).InstanceId);
        s.Step(); Assert.True(s.Events.Any(x => x.Kind == BattleEventKind.EffectBlocked));
        for (int a = 0; a < 12; a++) for (int b = 0; b < 12; b++) Assert.AreEqual(FormationRules.Adjacent(a, b), FormationRules.Adjacent(b, a));
        Assert.True(FormationRules.Adjacent(0, 4)); Assert.False(FormationRules.Adjacent(0, 11));
    }

    [Test] public void ImmunityFrostShieldCleanseDispelAndIntangibleUseSameEffectBoundary() {
        var e = Unit(TeamSide.Enemy); e.statuses.Add(Status("Immunity"));
        var s = new BattleSession(Data(Apply("Poison", .1f)), new() { Unit(TeamSide.Player) }, new() { e }); s.Step();
        Assert.AreEqual(1, State(s, TeamSide.Enemy).Statuses.Count);
        e.statuses.Clear(); e.shield = 50; e.statuses.Add(Status("FrostShield"));
        s = new BattleSession(Data(Apply("Freeze")), new() { Unit(TeamSide.Player) }, new() { e }); s.Step();
        Assert.AreEqual(1, State(s, TeamSide.Enemy).Statuses.Count);
        var p = Unit(TeamSide.Player); p.statuses.Add(Status("Poison", .01f)); p.statuses.Add(Status("DEFUp", .1f));
        var data = Data(new() { type = "Cleanse", target = "Self" }, new() { type = "Dispel" });
        s = new BattleSession(data, new() { p }, new() { e }); s.Step();
        Assert.AreEqual("DEFUp", State(s, TeamSide.Player).Statuses.Single().Name); Assert.IsEmpty(State(s, TeamSide.Enemy).Statuses);
        e.statuses.Add(Status("Intangible")); data = Data(); data.Skills["basic"].target = "AllEnemies";
        s = new BattleSession(data, new() { p }, new() { e }); s.Step(); Assert.AreEqual(1000, State(s, TeamSide.Enemy).Hp);
    }

    [Test] public void CharmForcesBasicAgainstAllyWithoutSpendingSignatureEnergy() {
        var p = Unit(TeamSide.Player); p.energy = 100; p.statuses.Add(Status("Charm")); var ally = Unit(TeamSide.Player, 11); ally.spd = 1;
        var s = new BattleSession(Data(), new() { p, ally }, new() { Unit(TeamSide.Enemy) }, auto: true); s.Step();
        Assert.AreEqual(900, State(s, TeamSide.Player, 11).Hp); Assert.AreEqual(1000, State(s, TeamSide.Enemy).Hp);
        Assert.AreEqual(100, State(s, TeamSide.Player).Energy);
        Assert.AreEqual("basic", s.Events.Single(x => x.Kind == BattleEventKind.SkillUsed).Detail);
    }

    [Test] public void PassiveReactionsDoNotRecurseAndDefinitionsAreDetached() {
        var p = Unit(TeamSide.Player); var e = Unit(TeamSide.Enemy);
        p.passives.Add(new() { id = "shield_on_hit", trigger = "DamageDealt", target = "Self", effects = new() { new() { type = "Shield", scale = .1f } } });
        e.passives.Add(new() { id = "counter", trigger = "DamageTaken", target = "Attacker", effects = new() { new() { type = "Damage", scale = .5f } } });
        var s = new BattleSession(Data(), new() { p }, new() { e }); p.passives[0].effects[0].scale = 9; s.Step();
        Assert.AreEqual(50, State(s, TeamSide.Player).Shield); Assert.AreEqual(1000, State(s, TeamSide.Player).Hp);
        Assert.AreEqual(2, s.Events.Count(x => x.Kind == BattleEventKind.PassiveTriggered));
        Assert.AreEqual(900, State(s, TeamSide.Enemy).Hp);
    }

    [Test] public void AttritionRecordsTheDefenderAsStatusSourceAndPermanentDurationIsExplicit() {
        var e = Unit(TeamSide.Enemy); e.role = "Taunt";
        var partner = Unit(TeamSide.Enemy, 11); partner.role = "DoT";
        var s = new BattleSession(Data(), new() { Unit(TeamSide.Player) }, new() { e, partner }); s.Step();
        var applied = s.Events.Single(x => x.Kind == BattleEventKind.StatusApplied);
        Assert.AreEqual(State(s, TeamSide.Enemy).InstanceId, applied.ActorId);
        Assert.AreEqual(applied.ActorId, applied.Target.Statuses.Single().SourceId);
        Assert.AreEqual(.02f, applied.Target.Statuses.Single().Potency);
        var data = Data(Apply("Cover", 1, -1)); data.Skills["basic"].target = "Self";
        s = new BattleSession(data, new() { Unit(TeamSide.Player) }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(-1, s.Events.Single(x => x.Kind == BattleEventKind.StatusApplied).Amount);
    }

    [Test] public void BiomeAndClassThresholdsUseHighestStatTierAndRetainEliteBonuses() {
        var team = Enumerable.Range(0, 5).Select(i => Unit(TeamSide.Player, i)).ToList();
        foreach (var u in team) { u.biome = "Forest"; u.classArchetype = "Tank"; u.def = 100; }
        var s = new BattleSession(Data(), team, new() { Unit(TeamSide.Enemy) });
        Assert.AreEqual(1100, State(s, TeamSide.Player).MaxHp);
        Assert.AreEqual(110, new FormationBonuses(team).Stat(team[0], "DEF"));
        var b = new FormationBonuses(team); team[0].biome = "Biome-less";
        Assert.AreEqual(.1f, b.Tier("Forest"));
        foreach (var u in team) { u.biome = "Wetlands"; u.classArchetype = "Mage"; }
        Assert.AreEqual(.25f, b.DotBonus, .0001);
        foreach (var u in team) u.classArchetype = "Tactician";
        Assert.AreEqual(1010, b.Stat(team[0], "SPD"));
        foreach (var u in team) u.classArchetype = "Support";
        Assert.AreEqual(.15f, b.HealingBonus, .0001);
    }

    [Test] public void BonusReadModelExcludesPlaceholderFamiliesAndReturnsDetachedLivingPairs() {
        var p = Unit(TeamSide.Player); p.role = "Wall";
        var ally = Unit(TeamSide.Player, 11); ally.role = "Medic";
        var s = new BattleSession(Data(), new() { p, ally }, new() { Unit(TeamSide.Enemy) });
        CollectionAssert.AreEqual(new[] { "role:Vanguard" }, s.GetFormationBonuses(TeamSide.Player));
        Assert.AreEqual("signature", s.GetState()[0].SignatureSkillId);
        var bonus = new FormationBonuses(new() { p, ally }); var before = bonus.Active;
        ally.hp = 0; Assert.IsEmpty(bonus.Active); CollectionAssert.AreEqual(new[] { "role:Vanguard" }, before);
    }

    [Test] public void RolePairsDisableWhenPartnerDiesAndBuffsGrantEnergy() {
        var p = Unit(TeamSide.Player); p.role = "Buffer";
        var battery = Unit(TeamSide.Player, 11); battery.role = "Battery"; battery.spd = 1;
        var data = Data(Apply("ATKUp", .2f)); data.Skills["basic"].target = "AllAllies";
        var s = new BattleSession(data, new() { p, battery }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(10, State(s, TeamSide.Player, 11).Energy);
        Assert.AreEqual(30, State(s, TeamSide.Player).Energy);
        battery.hp = 0;
        s = new BattleSession(data, new() { p, battery }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(20, State(s, TeamSide.Player).Energy);
        p.role = "Grunt"; battery.role = "Wall"; battery.hp = 600;
        Assert.AreEqual(.3f, new FormationBonuses(new() { p, battery }).Reduction(p));
        battery.hp = 500; Assert.AreEqual(0, new FormationBonuses(new() { p, battery }).Reduction(p));
    }

    [Test] public void StatSheetExamplesLoadWithCanonicalStatsAndRunWithObservablePassives() {
        var data = new GameData(); data.LoadAll(); var examples = new CombatExampleCatalog(data);
        var players = examples.CreateTeam(); var opponents = examples.CreateTeam(side: TeamSide.Enemy);
        Assert.AreEqual(190, players[0].maxHp); Assert.AreEqual("Wall", players[0].role);
        var s = new BattleSession(examples.Data, players, opponents, seed: 53, maxActions: 60, auto: true);
        while (s.Outcome == BattleOutcome.Running) s.Step();
        Assert.True(s.Events.Any(x => x.Kind == BattleEventKind.PassiveTriggered && x.Detail == "gilded_bash"));
        Assert.True(s.Events.Any(x => x.Kind == BattleEventKind.PassiveTriggered && x.Detail == "shell_up"));
        Assert.True(s.Events.Any(x => x.Kind == BattleEventKind.Redirected));
        Assert.True(s.Events.Any(x => x.Kind == BattleEventKind.SynergyActivated && x.Detail == "Fortress") ||
            s.Events.Any(x => x.Kind == BattleEventKind.Shield && x.Detail == "Fortress"));
        Assert.False(data.Characters.ContainsKey("example_golden"));
    }

    [Test] public void PeatShieldUsesActualGrantedEnergyAndCanonicalJsonHasNoCompetingStats() {
        var data = new GameData(); data.LoadAll(); var examples = new CombatExampleCatalog(data);
        var players = examples.CreateTeam();
        foreach (var u in players) u.spd = 1;
        var peat = players.Single(u => u.role == "Battery"); peat.spd = 1000; peat.energy = 100;
        players[0].hp = 1; players[0].energy = 90;
        var s = new BattleSession(examples.Data, players, new() { Unit(TeamSide.Enemy) }, auto: true); s.Step();
        Assert.AreEqual(100, State(s, TeamSide.Player).Energy);
        var shield = s.Events.Single(e => e.Kind == BattleEventKind.Shield && e.Detail == "ritual_staff");
        Assert.AreEqual((int)MathF.Round(State(s, TeamSide.Player, 8).MaxHp * .1f), shield.Amount);
        var file = JsonLoader.LoadFromResources<CombatExamplesFile>("GameData/combat_examples");
        Assert.True(file.characters.All(GameDataValidator.HasNoStatOverrides));
        file.characters[0].baseStats = new StatBlock { hp = 1 };
        Assert.False(GameDataValidator.HasNoStatOverrides(file.characters[0]));
    }

    [TestCase("Duelist", "CC", "Root", 0, 875)]
    [TestCase("AoE", "Saboteur", "Poison", 0, 880)]
    [TestCase("Stalker", "Stealth", "Marked", 3000, 900)]
    [TestCase("Nuker", "Scout", "Marked", 0, 900)]
    public void OffensiveRolePairsResolveRealHits(string role, string partner, string status, int defense, int expectedHp) {
        var p = Unit(TeamSide.Player); p.role = role;
        var ally = Unit(TeamSide.Player, 11); ally.role = partner; ally.spd = 1;
        var e = Unit(TeamSide.Enemy); e.def = defense;
        var mark = Status(status); mark.sourceRole = "Saboteur"; e.statuses.Add(mark);
        if (role == "Stalker") p.statuses.Add(Status("Stealth"));
        if (role == "Nuker") { e.statuses.Add(Status("Stealth")); e.statuses.Add(Status("Cloak")); }
        var s = new BattleSession(Data(), new() { p, ally }, new() { e }); s.Step();
        Assert.AreEqual(expectedHp, State(s, TeamSide.Enemy).Hp);
        if (role == "Stalker") Assert.False(State(s, TeamSide.Player).Statuses.Any(x => x.Name == "Stealth"));
    }

    [Test] public void CaptainBatteryBerserkerAndPurifierCoverApplySourceBonuses() {
        var captain = Unit(TeamSide.Player); captain.role = "Captain";
        var battery = Unit(TeamSide.Player, 1); battery.role = "Battery"; battery.spd = 1;
        var berserker = Unit(TeamSide.Player, 2); berserker.role = "Berserker"; berserker.spd = 1;
        var data = Data(Apply("ATKUp", .2f)); data.Skills["basic"].target = "AllAllies";
        var s = new BattleSession(data, new() { captain, battery, berserker }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(.25f, State(s, TeamSide.Player, 2).Statuses.Single().Potency);
        Assert.AreEqual(5, State(s, TeamSide.Player, 2).Energy); Assert.AreEqual(25, State(s, TeamSide.Player).Energy);
        captain.role = "Purifier"; battery.role = "Cover"; battery.statuses.Add(Status("Poison", .1f));
        data = Data(new EffectDef { type = "Cleanse", target = "AllAllies" });
        s = new BattleSession(data, new() { captain, battery }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(100, State(s, TeamSide.Player, 1).Shield); Assert.IsEmpty(State(s, TeamSide.Player, 1).Statuses);
    }

    [Test] public void CoverSurvivorReducesInterceptedHitAndDirectHitsAlsoGrantEnergy() {
        var e = Unit(TeamSide.Enemy); var cover = Unit(TeamSide.Enemy, 1); cover.role = "Cover"; cover.statuses.Add(Status("Cover", 1));
        var survivor = Unit(TeamSide.Enemy, 11); survivor.role = "Survivor";
        var s = new BattleSession(Data(), new() { Unit(TeamSide.Player) }, new() { e, cover, survivor }); s.Step();
        Assert.AreEqual(920, State(s, TeamSide.Enemy, 1).Hp); Assert.AreEqual(10, State(s, TeamSide.Enemy, 11).Energy);
        s = new BattleSession(Data(), new() { Unit(TeamSide.Player) }, new() { cover, survivor }); s.Step();
        Assert.AreEqual(900, State(s, TeamSide.Enemy, 1).Hp); Assert.AreEqual(10, State(s, TeamSide.Enemy, 11).Energy);
    }

    [Test] public void HealingPairsAndSupportEliteGrantOnlyOnPositiveHealing() {
        var p = Unit(TeamSide.Player); p.role = "Medic";
        var ally = Unit(TeamSide.Player, 1); ally.role = "Brawler"; ally.spd = 1; ally.hp = 500;
        var data = Data(new EffectDef { type = "Heal", stat = "MaxHP", scale = .1f, target = "AllAllies" });
        var s = new BattleSession(data, new() { p, ally }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(600, State(s, TeamSide.Player, 1).Hp); Assert.AreEqual(.15f, State(s, TeamSide.Player, 1).Statuses.Single().Potency);
        ally.role = "Wall"; ally.def = 100;
        s = new BattleSession(data, new() { p, ally }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(620, State(s, TeamSide.Player, 1).Hp);
        Assert.AreEqual(105, new FormationBonuses(new() { p, ally }).Stat(ally, "DEF"));
        var team = Enumerable.Range(0, 4).Select(i => Unit(TeamSide.Player, i)).ToList();
        foreach (var u in team) { u.classArchetype = "Support"; u.spd = 1; }
        team[0].spd = 1000; team[1].hp = 500;
        s = new BattleSession(data, team, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(615, State(s, TeamSide.Player, 1).Hp); Assert.AreEqual(5, State(s, TeamSide.Player, 1).Energy);
        Assert.AreEqual(20, State(s, TeamSide.Player).Energy);
    }

    [Test] public void CriticalDefenseBypassShieldBypassAndEliteExecutionerCompose() {
        var p = Unit(TeamSide.Player); p.classArchetype = "Assassin"; p.statuses.Add(Status("Luminescence", 1));
        var ally = Unit(TeamSide.Player, 11); ally.classArchetype = "Assassin"; ally.spd = 1;
        var e = Unit(TeamSide.Enemy); e.def = 3000; e.shield = 500;
        var data = Data(new EffectDef { type = "Damage", scale = 1, ignoreDefense = 1, ignoreShield = true });
        var s = new BattleSession(data, new() { p, ally }, new() { e }); s.Step();
        Assert.AreEqual(830, State(s, TeamSide.Enemy).Hp); Assert.AreEqual(500, State(s, TeamSide.Enemy).Shield);
        var team = Enumerable.Range(0, 4).Select(i => Unit(TeamSide.Player, i)).ToList();
        foreach (var u in team) { u.classArchetype = "DPS"; u.spd = 1; }
        team[0].spd = 1000; team[0].statuses.Add(Status("Luminescence", 1)); e.hp = 300; e.shield = 0; e.def = 0;
        s = new BattleSession(Data(), team, new() { e }); s.Step(); Assert.AreEqual(120, State(s, TeamSide.Enemy).Hp);
    }

    [Test] public void KitchenDurationCooldownAndUtilityEffectsHaveExplicitBounds() {
        var team = Enumerable.Range(0, 5).Select(i => Unit(TeamSide.Player, i)).ToList();
        foreach (var u in team) { u.biome = "Kitchen"; u.spd = 1; }
        team[0].spd = 1000; team[0].energy = 100;
        var data = Data(); data.Skills["signature"].effects = new() { Apply("DEFUp", .1f, 1) };
        var s = new BattleSession(data, team, new() { Unit(TeamSide.Enemy) }, auto: true); s.Step();
        Assert.AreEqual(2, State(s, TeamSide.Player).SignatureCooldown); Assert.AreEqual(2, State(s, TeamSide.Player).Statuses.Single().RemainingTurns);
        var p = Unit(TeamSide.Player); p.ultCdRemaining = 3;
        data = Data(new() { type = "Energy", potency = 200 }, new() { type = "Cooldown", potency = 99 }, new() { type = "Gauge", potency = 500 });
        data.Skills["basic"].target = "Self";
        s = new BattleSession(data, new() { p }, new() { Unit(TeamSide.Enemy) }); s.Step();
        Assert.AreEqual(100, State(s, TeamSide.Player).Energy); Assert.AreEqual(0, State(s, TeamSide.Player).SignatureCooldown); Assert.AreEqual(500, State(s, TeamSide.Player).Gauge);
    }

    [Test] public void CatalogValidationRejectsUnknownRulesAndInvalidMovementOrPassiveTriggers() {
        var skill = Data().Skills["basic"];
        skill.effects[0] = Apply("NotAStatus"); Assert.Throws<InvalidOperationException>(() => GameDataValidator.ValidateSkill(skill));
        skill.effects[0] = new() { type = "Move", slot = 12 }; Assert.Throws<InvalidOperationException>(() => GameDataValidator.ValidateSkill(skill));
        skill.effects[0] = new() { type = "Damage", stat = "MaxHP" }; Assert.Throws<InvalidOperationException>(() => GameDataValidator.ValidateSkill(skill));
        Assert.Throws<InvalidOperationException>(() => GameDataValidator.ValidatePassives("x", new() { new() { id = "p", trigger = "Nope", target = "Self" } }));
        Assert.Throws<InvalidOperationException>(() => GameDataValidator.ValidatePassives("x", new() { new() { id = "p", trigger = "BattleStart", target = "Attacker" } }));
    }
}
