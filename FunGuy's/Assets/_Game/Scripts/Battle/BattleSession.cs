using System;
using System.Collections.Generic;
using System.Linq;

public enum BattleOutcome { Running, Victory, Defeat, Draw, Timeout }
public enum BattleEventKind {
    BattleStarted, ActionStarted, GaugeChanged, EnergyChanged, CooldownChanged, SkillUsed,
    Damage, Heal, Shield, StatusApplied, StatusRefreshed, StatusTicked, StatusExpired,
    ActionSkipped, UnitDied, ActionCompleted, SignatureQueued, SignatureCancelled, AutoChanged, BattleEnded,
    Miss, Critical, Redirected, Moved, PassiveTriggered, SynergyActivated, EffectBlocked
}
public enum BattleCommandKind { QueueSignature, CancelSignature, SetAuto }

public sealed class BattleStatusState {
    public string Name { get; }
    public int RemainingTurns { get; }
    public float Potency { get; }
    public int Stacks { get; }
    public string SourceId { get; }
    internal BattleStatusState(StatusInstance value) {
        Name = value.status; RemainingTurns = value.remainingTurns; Potency = value.potency; Stacks = value.stacks; SourceId = value.sourceId;
    }
}

// Detached immutable presentation state. Content IDs need not be unique, instance IDs must be.
public sealed class BattleFighterState {
    public string InstanceId { get; }
    public string ContentId { get; }
    public string Name { get; }
    public TeamSide Side { get; }
    public int Slot { get; }
    public int Hp { get; }
    public int MaxHp { get; }
    public int Shield { get; }
    public int Energy { get; }
    public int MaxEnergy { get; }
    public float Gauge { get; }
    public int SignatureCooldown { get; }
    public string BasicSkillId { get; }
    public string SignatureSkillId { get; }
    public string UltimateSkillId { get; }
    public IReadOnlyList<BattleStatusState> Statuses { get; }
    internal BattleFighterState(string instanceId, CombatUnit unit) {
        InstanceId = instanceId; ContentId = unit.id; Name = unit.name; Side = unit.side; Slot = unit.formationSlot;
        Hp = unit.hp; MaxHp = unit.maxHp; Shield = unit.shield; Energy = unit.energy; MaxEnergy = unit.maxEnergy;
        Gauge = unit.actionGauge; SignatureCooldown = unit.ultCdRemaining;
        BasicSkillId = unit.basicSkillId; SignatureSkillId = unit.ultSkillId; UltimateSkillId = unit.ultimateSkillId;
        Statuses = Array.AsReadOnly(unit.statuses.Select(s => new BattleStatusState(s)).ToArray());
    }
}

public sealed class BattleEvent {
    public int Sequence { get; }
    public int Action { get; }
    public BattleEventKind Kind { get; }
    public string ActorId { get; }
    public BattleFighterState Target { get; }
    public int Amount { get; }
    public string Detail { get; }
    public BattleOutcome Outcome { get; }
    internal BattleEvent(int sequence, int action, BattleEventKind kind, string actorId,
        BattleFighterState target, int amount, string detail, BattleOutcome outcome) {
        Sequence = sequence; Action = action; Kind = kind; ActorId = actorId; Target = target;
        Amount = amount; Detail = detail; Outcome = outcome;
    }
}

public sealed class BattleCommandRecord {
    public int Sequence { get; }
    public int AfterAction { get; }
    public BattleCommandKind Kind { get; }
    public string FighterId { get; }
    public bool AutoEnabled { get; }
    internal BattleCommandRecord(int sequence, int action, BattleCommandKind kind, string fighterId, bool auto) {
        Sequence = sequence; AfterAction = action; Kind = kind; FighterId = fighterId; AutoEnabled = auto;
    }
}

// One wave. No Unity, wall clock, animation timing, wallet writes or callbacks into presentation.
public sealed class BattleSession {
    public const string RulesVersion = "battle-actions-v2";
    private readonly List<CombatUnit> player;
    private readonly List<CombatUnit> enemy;
    private readonly Dictionary<CombatUnit, string> ids = new();
    private readonly HashSet<string> queued = new(StringComparer.Ordinal);
    private readonly List<BattleEvent> events = new();
    private readonly List<BattleCommandRecord> commands = new();
    private readonly BattleSim engine;
    private readonly GameData data;
    private readonly int maxActions;
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Running;
    public int CompletedActions { get; private set; }
    public bool AutoEnabled { get; private set; }
    public int? Seed { get; private set; }
    public IReadOnlyList<BattleEvent> Events => events.AsReadOnly();
    public IReadOnlyList<BattleCommandRecord> Commands => commands.AsReadOnly();
    public IReadOnlyList<BattleFighterState> InitialState { get; }

    public BattleSession(GameData data, List<CombatUnit> player, List<CombatUnit> enemy, int seed = 0,
        int maxActions = 200, bool auto = false, string encounterId = "battle", int wave = 1)
        : this(data, player, enemy, new Random(seed), maxActions, auto, encounterId, wave, true) { Seed = seed; }

    internal BattleSession(GameData data, List<CombatUnit> player, List<CombatUnit> enemy, Random rng,
        int maxActions, bool auto, string encounterId, int wave, bool cloneInputs) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (maxActions < 0 || wave < 1) throw new ArgumentOutOfRangeException(nameof(maxActions));
        if (string.IsNullOrWhiteSpace(encounterId)) throw new ArgumentException("Encounter ID is required.");
        ValidateSide(player, TeamSide.Player); ValidateSide(enemy, TeamSide.Enemy);
        if (player.Concat(enemy).Distinct().Count() != player.Count + enemy.Count)
            throw new ArgumentException("A fighter instance cannot occupy multiple spaces.");
        this.data = SnapshotSkills(data); this.maxActions = maxActions; AutoEnabled = auto;
        this.player = cloneInputs ? player.Select(Clone).ToList() : player;
        this.enemy = cloneInputs ? enemy.Select(Clone).ToList() : enemy;
        FormationRules.AssignBattleSlots(this.player); FormationRules.AssignBattleSlots(this.enemy);
        foreach (var unit in this.player) {
            unit.instanceId ??= $"{encounterId}/player/{unit.formationSlot}";
            ids.Add(unit, unit.instanceId);
        }
        foreach (var unit in this.enemy) {
            unit.instanceId = $"{encounterId}/wave/{wave}/enemy/{unit.formationSlot}";
            ids.Add(unit, unit.instanceId);
        }
        engine = new BattleSim(this.data, rng) { Trace = Record };
        InitialState = GetState();
        Record(BattleEventKind.BattleStarted, null, null, 0, "wave:" + wave);
        engine.Initialize(this.player, this.enemy);
        EvaluateOutcome();
    }

    public IReadOnlyList<BattleFighterState> GetState() => Array.AsReadOnly(player.Concat(enemy)
        .OrderBy(u => u.side).ThenBy(u => u.formationSlot).Select(u => new BattleFighterState(ids[u], u)).ToArray());

    public bool IsSignatureQueued(string fighterId) => fighterId != null && queued.Contains(fighterId);
    public IReadOnlyList<string> GetFormationBonuses(TeamSide side) => engine.GetFormationBonuses(side);

    public bool QueueSignature(string fighterId) {
        var unit = FindPlayer(fighterId);
        if (Outcome != BattleOutcome.Running || unit == null || unit.hp <= 0 || queued.Contains(fighterId) ||
            unit.ultSkillId == unit.basicSkillId || !data.Skills.ContainsKey(unit.ultSkillId ?? "")) return false;
        queued.Add(fighterId);
        RecordCommand(BattleCommandKind.QueueSignature, fighterId);
        Record(BattleEventKind.SignatureQueued, unit, unit, 0, unit.ultSkillId);
        return true;
    }

    public bool CancelSignature(string fighterId) {
        var unit = FindPlayer(fighterId);
        if (Outcome != BattleOutcome.Running || unit == null || !queued.Remove(fighterId)) return false;
        RecordCommand(BattleCommandKind.CancelSignature, fighterId);
        Record(BattleEventKind.SignatureCancelled, unit, unit, 0, "player");
        return true;
    }

    public bool SetAuto(bool enabled) {
        if (Outcome != BattleOutcome.Running || enabled == AutoEnabled) return false;
        AutoEnabled = enabled;
        RecordCommand(BattleCommandKind.SetAuto, null);
        Record(BattleEventKind.AutoChanged, null, null, enabled ? 1 : 0, "player");
        return true;
    }

    // Call only between animation batches. A terminal session is a stable no-op.
    public IReadOnlyList<BattleEvent> Step() {
        if (Outcome != BattleOutcome.Running) return Array.Empty<BattleEvent>();
        int firstEvent = events.Count;
        CompletedActions++;
        engine.ExecuteNextAction(player, enemy,
            unit => unit.side == TeamSide.Enemy || AutoEnabled || queued.Contains(ids[unit]),
            unit => queued.Remove(ids[unit]));
        foreach (var unit in player.Where(u => u.hp <= 0))
            if (queued.Remove(ids[unit])) Record(BattleEventKind.SignatureCancelled, unit, unit, 0, "dead");
        EvaluateOutcome();
        return Array.AsReadOnly(events.Skip(firstEvent).ToArray());
    }

    private void EvaluateOutcome() {
        bool p = player.Any(u => u.hp > 0), e = enemy.Any(u => u.hp > 0);
        Outcome = !p && !e ? BattleOutcome.Draw : !p ? BattleOutcome.Defeat : !e ? BattleOutcome.Victory :
            CompletedActions >= maxActions ? BattleOutcome.Timeout : BattleOutcome.Running;
        if (Outcome == BattleOutcome.Running) return;
        foreach (var unit in player.Where(u => queued.Contains(ids[u])))
            Record(BattleEventKind.SignatureCancelled, unit, unit, 0, "battle-ended");
        queued.Clear();
        Record(BattleEventKind.BattleEnded, null, null, 0, Outcome.ToString());
    }

    private CombatUnit FindPlayer(string fighterId) => player.FirstOrDefault(u => ids[u] == fighterId);
    private void RecordCommand(BattleCommandKind kind, string fighterId) =>
        commands.Add(new BattleCommandRecord(commands.Count + 1, CompletedActions, kind, fighterId, AutoEnabled));
    private void Record(BattleEventKind kind, CombatUnit actor, CombatUnit target, int amount, string detail) =>
        events.Add(new BattleEvent(events.Count + 1, CompletedActions, kind, actor == null ? null : ids[actor],
            target == null ? null : new BattleFighterState(ids[target], target), amount, detail, Outcome));
    private static void ValidateSide(List<CombatUnit> units, TeamSide side) {
        if (units == null || units.Count > FormationRules.Capacity || units.Any(u => u == null || u.side != side ||
            u.statuses == null || u.statuses.Any(s => s == null))) throw new ArgumentException("Invalid battle side.");
    }
    private static CombatUnit Clone(CombatUnit u) => new() {
        side = u.side, id = u.id, name = u.name, level = u.level, formationSlot = u.formationSlot,
        biome = u.biome, classArchetype = u.classArchetype, role = u.role, maxHp = u.maxHp, hp = u.hp,
        atk = u.atk, def = u.def, spd = u.spd, pot = u.pot, basicSkillId = u.basicSkillId, ultSkillId = u.ultSkillId, ultimateSkillId = u.ultimateSkillId,
        ultCdRemaining = u.ultCdRemaining, ultimateCdRemaining = u.ultimateCdRemaining, energy = u.energy, maxEnergy = u.maxEnergy,
        actionGauge = u.actionGauge, shield = u.shield, turnsTaken = u.turnsTaken,
        passives = CombatEffectRules.ClonePassives(u.passives),
        statuses = u.statuses.Select(s => new StatusInstance {
            status = s.status, remainingTurns = s.remainingTurns, potency = s.potency, stacks = s.stacks,
            sourceId = s.sourceId, sourceRole = s.sourceRole, damageMultiplier = s.damageMultiplier }).ToList()
    };

    internal static GameData SnapshotSkills(GameData source) => new() {
        Skills = source.Skills.ToDictionary(p => p.Key, p => new SkillDef {
            id = p.Value.id, name = p.Value.name, target = p.Value.target,
            cooldown = p.Value.cooldown, energyCost = p.Value.energyCost,
            effects = p.Value.effects?.Select(CombatEffectRules.Clone).ToList()
        }, StringComparer.Ordinal)
    };
}
