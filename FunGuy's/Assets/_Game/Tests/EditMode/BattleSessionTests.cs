using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class BattleSessionTests {
    private static GameData Data() => new() { Skills = new() {
        ["basic"] = new SkillDef { id = "basic", target = "EnemyFront", effects = new() { new() { type = "Damage", scale = 1 } } },
        ["signature"] = new SkillDef { id = "signature", target = "EnemyFront", energyCost = 100, cooldown = 2,
            effects = new() { new() { type = "Damage", scale = 3 } } }
    } };
    private static CombatUnit Unit(TeamSide side, int slot = 0) => new() {
        id = "same-content-id", name = "Fighter", side = side, formationSlot = slot, hp = 1000, maxHp = 1000,
        atk = 10, pot = 10, spd = side == TeamSide.Player ? 1000 : 1, biome = "Kitchen",
        basicSkillId = "basic", ultSkillId = "signature"
    };
    private static string PlayerId(BattleSession session) => session.InitialState.Single(s => s.Side == TeamSide.Player).InstanceId;

    [Test]
    public void ManualSignatureWaitsThroughSilenceCooldownAndCostsEnergyOnlyWhenCast() {
        var p = Unit(TeamSide.Player); p.energy = 80; p.ultCdRemaining = 2;
        p.statuses.Add(new() { status = "Silence", remainingTurns = 1 });
        var session = new BattleSession(Data(), new() { p }, new() { Unit(TeamSide.Enemy) });
        string id = PlayerId(session);
        Assert.True(session.QueueSignature(id));
        Assert.False(session.QueueSignature(id));
        Assert.False(session.QueueSignature(session.InitialState.Single(s => s.Side == TeamSide.Enemy).InstanceId));
        var first = session.Step();
        Assert.AreEqual("basic", first.Single(e => e.Kind == BattleEventKind.SkillUsed).Detail);
        Assert.True(session.IsSignatureQueued(id));
        Assert.AreEqual(100, session.GetState().Single(s => s.Side == TeamSide.Player).Energy);
        var second = session.Step();
        Assert.AreEqual("signature", second.Single(e => e.Kind == BattleEventKind.SkillUsed).Detail);
        Assert.False(session.IsSignatureQueued(id));
        Assert.AreEqual(5, session.GetState().Single(s => s.Side == TeamSide.Player).Energy);
        Assert.AreEqual(2, session.GetState().Single(s => s.Side == TeamSide.Player).SignatureCooldown);
        Assert.AreEqual(1, session.InitialState[0].Statuses[0].RemainingTurns);
        Assert.AreEqual(80, p.energy); Assert.AreEqual(2, p.ultCdRemaining);
        Assert.Less(second.Single(e => e.Kind == BattleEventKind.EnergyChanged && e.Amount == -100).Sequence,
            second.Single(e => e.Kind == BattleEventKind.SkillUsed).Sequence);
    }

    [Test]
    public void AutoToggleAndCancellationUseSameActionEligibilityAndEnemyRemainsAutomatic() {
        var p = Unit(TeamSide.Player); p.energy = 100;
        var session = new BattleSession(Data(), new() { p }, new() { Unit(TeamSide.Enemy) });
        string id = PlayerId(session);
        Assert.True(session.QueueSignature(id)); Assert.True(session.CancelSignature(id));
        Assert.False(session.CancelSignature(id));
        Assert.AreEqual("basic", session.Step().Single(e => e.Kind == BattleEventKind.SkillUsed).Detail);
        Assert.True(session.SetAuto(true)); Assert.False(session.SetAuto(true));
        Assert.AreEqual("signature", session.Step().Single(e => e.Kind == BattleEventKind.SkillUsed).Detail);
        Assert.AreEqual(3, session.Commands.Count);
        p.spd = 1;
        var enemy = Unit(TeamSide.Enemy); enemy.spd = 1000; enemy.energy = 100;
        var enemySession = new BattleSession(Data(), new() { p }, new() { enemy });
        Assert.AreEqual("signature", enemySession.Step().Single(e => e.Kind == BattleEventKind.SkillUsed).Detail);
    }

    [Test]
    public void TerminalOutcomesAreDistinctAndFurtherCommandsAndStepsAreNoOps() {
        var p = Unit(TeamSide.Player); var e = Unit(TeamSide.Enemy); e.hp = 10;
        var victory = new BattleSession(Data(), new() { p }, new() { e }, maxActions: 1);
        victory.Step(); Assert.AreEqual(BattleOutcome.Victory, victory.Outcome);
        int count = victory.Events.Count;
        Assert.IsEmpty(victory.Step()); Assert.False(victory.SetAuto(true));
        Assert.False(victory.QueueSignature(PlayerId(victory))); Assert.AreEqual(count, victory.Events.Count);
        Assert.AreEqual(BattleEventKind.BattleEnded, victory.Events.Last().Kind);
        Assert.AreEqual(BattleOutcome.Victory, victory.Events.Last().Outcome);
        e.hp = 1000;
        Assert.AreEqual(BattleOutcome.Timeout, new BattleSession(Data(), new() { p }, new() { e }, maxActions: 0).Outcome);
        p.hp = 0;
        Assert.AreEqual(BattleOutcome.Defeat, new BattleSession(Data(), new() { p }, new() { e }).Outcome);
        e.hp = 0;
        Assert.AreEqual(BattleOutcome.Draw, new BattleSession(Data(), new() { p }, new() { e }).Outcome);
    }

    [Test]
    public void ReflectionDoubleKnockoutAndLethalStatusEmitDeathsWithoutExtraActions() {
        var p = Unit(TeamSide.Player); p.hp = 5;
        var e = Unit(TeamSide.Enemy); e.hp = 10;
        e.statuses.Add(new() { status = "Thorns", potency = .5f, remainingTurns = 2 });
        var draw = new BattleSession(Data(), new() { p }, new() { e });
        draw.Step(); Assert.AreEqual(BattleOutcome.Draw, draw.Outcome);
        Assert.AreEqual(2, draw.Events.Count(x => x.Kind == BattleEventKind.UnitDied));
        Assert.AreEqual(1, draw.Events.Count(x => x.Kind == BattleEventKind.SkillUsed));
        p.statuses.Add(new() { status = "Poison", potency = .1f, remainingTurns = 1, stacks = 1 });
        var poison = new BattleSession(Data(), new() { p }, new() { Unit(TeamSide.Enemy) });
        poison.QueueSignature(PlayerId(poison)); poison.Step();
        Assert.AreEqual(BattleOutcome.Defeat, poison.Outcome);
        Assert.False(poison.Events.Any(x => x.Kind == BattleEventKind.SkillUsed));
        Assert.AreEqual(1, poison.Events.Count(x => x.Kind == BattleEventKind.UnitDied));
        Assert.True(poison.Events.Any(x => x.Kind == BattleEventKind.SignatureCancelled));
    }

    [Test]
    public void InstanceIdsDistinguishRepeatedEnemiesAndPreservePlayerAcrossWaves() {
        var first = new BattleSession(Data(), new() { Unit(TeamSide.Player) },
            new() { Unit(TeamSide.Enemy, 0), Unit(TeamSide.Enemy, 11) }, encounterId: "stage", wave: 1);
        var second = new BattleSession(Data(), new() { Unit(TeamSide.Player) },
            new() { Unit(TeamSide.Enemy, 0) }, encounterId: "stage", wave: 2);
        Assert.AreEqual(3, first.InitialState.Select(s => s.InstanceId).Distinct().Count());
        Assert.AreEqual(PlayerId(first), PlayerId(second));
        Assert.AreNotEqual(first.InitialState[1].InstanceId, second.InitialState[1].InstanceId);
    }

    [Test]
    public void SessionSnapshotsUnitsSkillsAndEarlierEventsAndResetsGaugeToSheetRule() {
        var data = Data(); var p = Unit(TeamSide.Player); p.spd = 600;
        var e = Unit(TeamSide.Enemy);
        var session = new BattleSession(data, new() { p }, new() { e });
        p.atk = 999; e.hp = 2; data.Skills["basic"].effects[0].scale = 99;
        var initial = session.GetState();
        session.Step();
        Assert.AreEqual(990, session.GetState().Single(s => s.Side == TeamSide.Enemy).Hp);
        Assert.AreEqual(0, session.GetState().Single(s => s.Side == TeamSide.Player).Gauge);
        Assert.AreEqual(1000, initial.Single(s => s.Side == TeamSide.Enemy).Hp);
        var damage = session.Events.Single(s => s.Kind == BattleEventKind.Damage);
        session.Step(); Assert.AreEqual(990, damage.Target.Hp);
    }

    [Test]
    public void UnityBlankOptionalEffectFieldsFallbackAfterStartTurnEnergyGeneration() {
        var data = Data();
        // ScriptableObject serialization produces empty strings for unset optional fields.
        // The equivalent JSON skill omits them and therefore produces null.
        data.Skills["basic"].effects[0].target = "";
        data.Skills["basic"].effects[0].stat = "";

        var p = Unit(TeamSide.Player);
        var e = Unit(TeamSide.Enemy);
        var session = new BattleSession(data, new() { p }, new() { e });

        var step = session.Step();

        var startEnergy = step.First(x => x.Kind == BattleEventKind.EnergyChanged &&
            x.Target?.Side == TeamSide.Player);
        Assert.AreEqual(20, startEnergy.Amount);
        Assert.AreEqual("basic", step.Single(x => x.Kind == BattleEventKind.SkillUsed).Detail);
        Assert.AreEqual(990, session.GetState().Single(x => x.Side == TeamSide.Enemy).Hp);
        Assert.AreEqual(25, session.GetState().Single(x => x.Side == TeamSide.Player).Energy);
        Assert.AreEqual(BattleOutcome.Running, session.Outcome);
    }

    [Test]
    public void HitEnergyIsPerDamagingEffectAndKillReplacesThatHitsDamageEnergy() {
        var data = Data(); data.Skills["basic"].effects.Add(new() { type = "Damage", scale = 1 });
        data.Skills["basic"].effects.Add(new() { type = "Damage", scale = 1 });
        var p = Unit(TeamSide.Player); var e = Unit(TeamSide.Enemy); e.hp = 25;
        var session = new BattleSession(data, new() { p }, new() { e }); session.Step();
        Assert.AreEqual(45, session.GetState().Single(s => s.Side == TeamSide.Player).Energy); // 20 + 5 + 5 + 15
        Assert.AreEqual(3, session.Events.Count(x => x.Kind == BattleEventKind.Damage));
        Assert.AreEqual(1, session.Events.Count(x => x.Kind == BattleEventKind.UnitDied));
    }

    [Test]
    public void AcceptedCommandLogReplaysSameEventsAndStateWithSameInputsAndSeed() {
        BattleSession Create() {
            var data = Data();
            data.Skills["basic"].target = "RandomEnemy2";
            data.Skills["basic"].effects.Add(new() { type = "ApplyStatus", status = "Poison", chance = .4f, duration = 2, potency = .01f });
            return new(data, new() { Unit(TeamSide.Player) }, new() { Unit(TeamSide.Enemy), Unit(TeamSide.Enemy, 11) }, seed: 73, maxActions: 8);
        }
        var first = Create(); string id = PlayerId(first);
        first.QueueSignature(id); first.Step(); first.CancelSignature(id); first.Step();
        first.SetAuto(true); first.Step(); first.SetAuto(false); first.QueueSignature(id);
        while (first.Outcome == BattleOutcome.Running) first.Step();
        var replay = Create();
        while (replay.Outcome == BattleOutcome.Running) {
            foreach (var command in first.Commands.Where(c => c.AfterAction == replay.CompletedActions)) {
                bool accepted = command.Kind switch {
                    BattleCommandKind.QueueSignature => replay.QueueSignature(command.FighterId),
                    BattleCommandKind.CancelSignature => replay.CancelSignature(command.FighterId),
                    _ => replay.SetAuto(command.AutoEnabled)
                };
                Assert.True(accepted);
            }
            replay.Step();
        }
        string Describe(BattleEvent e) => $"{e.Sequence}|{e.Action}|{e.Kind}|{e.ActorId}|{e.Target?.InstanceId}|{e.Target?.Hp}|{e.Target?.Energy}|{e.Amount}|{e.Detail}|{e.Outcome}";
        CollectionAssert.AreEqual(first.Events.Select(Describe), replay.Events.Select(Describe));
        CollectionAssert.AreEqual(Enumerable.Range(1, first.Events.Count), first.Events.Select(e => e.Sequence));
        Assert.AreEqual(first.Outcome, replay.Outcome);
    }

    [Test]
    public void AutomaticSessionAndLegacyRunnerHaveIdenticalCombatResults() {
        var data = Data(); var p = Unit(TeamSide.Player); var e = Unit(TeamSide.Enemy);
        var session = new BattleSession(data, new() { p }, new() { e }, seed: 73, maxActions: 20, auto: true);
        while (session.Outcome == BattleOutcome.Running) session.Step();
        bool won = new BattleSim(data, 73).RunBattle(new() { p }, new() { e }, 20);
        Assert.AreEqual(won, session.Outcome == BattleOutcome.Victory);
        CollectionAssert.AreEqual(new[] { p.hp, e.hp }, session.GetState().Select(s => s.Hp));
        CollectionAssert.AreEqual(new[] { p.energy, e.energy }, session.GetState().Select(s => s.Energy));
    }

    [TestCase("Stun")]
    [TestCase("Freeze")]
    public void SkippedActionStillTicksCooldownEnergyAndDurationWhileKeepingQueuedSkill(string control) {
        var p = Unit(TeamSide.Player); p.energy = 80; p.ultCdRemaining = 2;
        p.statuses.Add(new() { status = control, remainingTurns = 1 });
        var session = new BattleSession(Data(), new() { p }, new() { Unit(TeamSide.Enemy) });
        session.QueueSignature(PlayerId(session));
        var step = session.Step();
        Assert.True(step.Any(e => e.Kind == BattleEventKind.ActionSkipped));
        Assert.False(step.Any(e => e.Kind == BattleEventKind.SkillUsed));
        var state = session.GetState().Single(s => s.Side == TeamSide.Player);
        Assert.AreEqual(100, state.Energy); Assert.AreEqual(1, state.SignatureCooldown); Assert.IsEmpty(state.Statuses);
        Assert.True(session.IsSignatureQueued(state.InstanceId));
        Assert.AreEqual("signature", session.Step().Single(e => e.Kind == BattleEventKind.SkillUsed).Detail);
    }

    [Test]
    public void HealShieldAndStatusEventsContainExactImmutablePostChangeState() {
        var data = Data(); data.Skills["basic"].target = "Self";
        data.Skills["basic"].effects = new() {
            new() { type = "Heal", scale = .2f }, new() { type = "Shield", scale = .1f },
            new() { type = "ApplyStatus", status = "Regen", chance = 1, duration = 2, potency = .1f }
        };
        var p = Unit(TeamSide.Player); p.maxHp = 100; p.hp = 50; p.pot = 100;
        var session = new BattleSession(data, new() { p }, new() { Unit(TeamSide.Enemy) });
        var step = session.Step();
        var heal = step.Single(e => e.Kind == BattleEventKind.Heal);
        var shield = step.Single(e => e.Kind == BattleEventKind.Shield);
        var status = step.Single(e => e.Kind == BattleEventKind.StatusApplied);
        Assert.AreEqual(20, heal.Amount); Assert.AreEqual(70, heal.Target.Hp); Assert.AreEqual(0, heal.Target.Shield);
        Assert.AreEqual(10, shield.Amount); Assert.AreEqual(10, shield.Target.Shield);
        Assert.AreEqual(2, status.Target.Statuses.Single().RemainingTurns);
        session.Step();
        Assert.AreEqual(70, heal.Target.Hp); Assert.AreEqual(2, status.Target.Statuses.Single().RemainingTurns);
    }
}
