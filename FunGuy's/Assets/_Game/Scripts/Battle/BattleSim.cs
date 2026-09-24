using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleSim {
  private const float ActionGaugeThreshold = 1000f;
  private const int StartTurnEnergyGain = 10;
  private const int DealDamageEnergyGain = 5;
  private const int TakeDamageEnergyGain = 10;
  private const int KillEnergyGain = 15;

  private readonly GameData _data;
  private readonly Random _rng;
  internal Action<BattleEventKind, CombatUnit, CombatUnit, int, string> Trace;
  private CombatUnit actingUnit;
  private string skillContext;

  public BattleSim(GameData data) : this(data, rng: null) { }

  public BattleSim(GameData data, int seed) : this(data, new Random(seed)) { }

  public BattleSim(GameData data, Random rng) {
    _data = data;
    _rng = rng ?? new Random();
  }

  public bool RunBattle(List<CombatUnit> player, List<CombatUnit> enemy, int maxTurns = 200) {
    return RunToCompletion(player, enemy, maxTurns).Outcome == BattleOutcome.Victory;
  }

  // Compatibility path for existing campaign/QA callers; interactive sessions default to manual signatures.
  internal BattleSession RunToCompletion(List<CombatUnit> player, List<CombatUnit> enemy,
      int maxActions = 200, string encounterId = "battle", int wave = 1) {
    var session = new BattleSession(_data, player, enemy, _rng, maxActions, true, encounterId, wave, false);
    while (session.Outcome == BattleOutcome.Running) session.Step();
    return session;
  }

  internal void ExecuteNextAction(List<CombatUnit> player, List<CombatUnit> enemy,
      Func<CombatUnit, bool> wantsSignature, Action<CombatUnit> signatureUsed) {
      var actor = NextActor(player, enemy);
      if (actor == null) return;
      actingUnit = actor; skillContext = null;
      coverUsed.Clear();
      redirectedHit.Clear();
      actor.turnsTaken++;
      Emit(BattleEventKind.ActionStarted, actor, actor);
      // Stat-sheet Spore Gauge resets to zero after reaching the threshold.
      actor.actionGauge = 0;
      Emit(BattleEventKind.GaugeChanged, actor, actor, 0, "action-reset");
      GainEnergy(actor, StartTurnEnergyGain);
      TickSignatureCooldown(actor);

      var activeStatuses = actor.statuses.ToArray();
      try {
        TurnBonuses(actor);
        Trigger(actor, "TurnStart", actor);
        bool turnSkipped = ApplyStartOfTurnStatuses(actor);
        if (actor.hp <= 0 || turnSkipped) {
          Emit(BattleEventKind.ActionSkipped, actor, actor, 0, actor.hp <= 0 ? "dead" : "controlled");
          return;
        }
        var opponents = actor.side == TeamSide.Player ? enemy : player;
        var allies = actor.side == TeamSide.Player ? player : enemy;
        if (!AnyAlive(opponents)) return;
        bool charmed = HasStatus(actor, "Charm") || (HasStatus(actor, "Confusion") && _rng.NextDouble() < .5);
        if (charmed) opponents = allies.Where(u => u != actor).ToList();
        if (!TrySelectSkill(actor, !charmed && wantsSignature(actor), out var selectedSkill, out bool usedUlt)) {
          Emit(BattleEventKind.ActionSkipped, actor, actor, 0, "no-skill"); return;
        }
        skillContext = selectedSkill.id;
        if (usedUlt) {
          signatureUsed(actor);
          if (selectedSkill.id == actor.ultimateSkillId) {
            SpendEnergy(actor, 100);
          } else {
            actor.ultCdRemaining = Bonus(actor).Biome("Kitchen") >= 5 && selectedSkill.cooldown > 0
              ? Math.Max(2, selectedSkill.cooldown - 1)
              : Math.Max(0, selectedSkill.cooldown);
            Emit(BattleEventKind.CooldownChanged, actor, actor, actor.ultCdRemaining, "signature");
          }
        }
        Emit(BattleEventKind.SkillUsed, actor, actor, usedUlt ? 1 : 0, selectedSkill.id);
        usingBasic = !usedUlt;
        ExecuteSkill(actor, allies, opponents, selectedSkill);
        if (actor.hp > 0 && usedUlt && selectedSkill.id == actor.ultSkillId && Bonus(actor).Class("Mage") >= 4 && _rng.NextDouble() < .1) {
          actor.ultCdRemaining = 0;
          Emit(BattleEventKind.CooldownChanged, actor, actor, 0, "Spell Weaver");
        }
      }
      finally {
        // A status lasts through its owner's action, including a skipped turn.
        // Statuses added by this action begin aging on the next owner turn.
        foreach (var status in activeStatuses) {
          if (!actor.statuses.Contains(status) || status.remainingTurns == -1) continue;
          status.remainingTurns--;
          if (status.remainingTurns <= 0) {
            actor.statuses.Remove(status);
            Emit(BattleEventKind.StatusExpired, actor, actor, 0, status.status);
          } else Emit(BattleEventKind.StatusTicked, actor, actor, status.remainingTurns, status.status);
        }
        Emit(BattleEventKind.ActionCompleted, actor, actor);
        actingUnit = null; skillContext = null;
      }
  }

  private bool AnyAlive(List<CombatUnit> team) => team.Any(u => u.hp > 0);

  private CombatUnit NextActor(List<CombatUnit> player, List<CombatUnit> enemy) {
    var alive = player.Concat(enemy).Where(u => u.hp > 0).ToList();
    if (alive.Count == 0) return null;

    float highestGauge = alive.Max(u => u.actionGauge);
    if (highestGauge < ActionGaugeThreshold) {
      int ticks = alive
        .Select(u => TicksUntilAction(u))
        .DefaultIfEmpty(1)
        .Min();

      foreach (var unit in alive) {
        unit.actionGauge += Stat(unit, "SPD") * ticks;
        Emit(BattleEventKind.GaugeChanged, null, unit, ticks, "time-advanced");
      }
    }

    return alive
      .OrderByDescending(u => u.actionGauge)
      .ThenByDescending(u => Stat(u, "SPD"))
      .ThenBy(u => u.side)
      .ThenBy(u => u.formationSlot)
      .FirstOrDefault();
  }

  private int TicksUntilAction(CombatUnit unit) {
    int speed = Stat(unit, "SPD");
    float needed = Math.Max(0f, ActionGaugeThreshold - unit.actionGauge);
    return Math.Max(1, (int)Math.Ceiling(needed / speed));
  }

  private void TickSignatureCooldown(CombatUnit unit) {
    if (unit.ultCdRemaining > 0) {
      unit.ultCdRemaining -= 1;
      Emit(BattleEventKind.CooldownChanged, unit, unit, unit.ultCdRemaining, "signature-turn-start");
    }
  }

  private bool TrySelectSkill(CombatUnit actor, bool wantsSignature, out SkillDef selectedSkill, out bool usedUlt) {
    selectedSkill = null;
    usedUlt = false;

    _ = _data.Skills.TryGetValue(actor.basicSkillId ?? string.Empty, out var basicSkill);
    _ = _data.Skills.TryGetValue(actor.ultSkillId ?? string.Empty, out var signatureSkill);
    _ = _data.Skills.TryGetValue(actor.ultimateSkillId ?? string.Empty, out var ultimateSkill);
    if (basicSkill == null && signatureSkill == null && ultimateSkill == null) return false;

    bool silenced = HasStatus(actor, "Silence");
    bool canCastUltimate = wantsSignature && !silenced &&
      ultimateSkill != null && ultimateSkill != basicSkill &&
      actor.energy >= 100;
    bool canCastSignature = wantsSignature && !silenced &&
      signatureSkill != null && signatureSkill != basicSkill &&
      actor.ultCdRemaining <= 0;

    selectedSkill = canCastUltimate ? ultimateSkill : canCastSignature ? signatureSkill : basicSkill;
    usedUlt = canCastUltimate || canCastSignature;
    return selectedSkill != null;
  }

  private bool ApplyStartOfTurnStatuses(CombatUnit u) {
    bool skipTurn = false;

    foreach (var s in u.statuses.ToList()) {
      if (u.hp <= 0) break;
      int stacks = Math.Max(1, s.stacks);
      string status = (s.status ?? string.Empty).Trim().ToLowerInvariant();

      if (status == "regen") Heal(u, u, (int)MathF.Round(u.maxHp * s.potency * stacks));
      if (status == "poison" || status == "burn") {
        int dot = (int)MathF.Round((status == "poison" ? u.maxHp : 1) * s.potency * stacks * s.damageMultiplier);
        DealPure(null, u, dot, status);
      }
      if (status == "freeze" || status == "stun") skipTurn = true;


    }

    return skipTurn;
  }

  private void ExecuteSkill(CombatUnit actor, List<CombatUnit> allies, List<CombatUnit> enemies, SkillDef skill) {
    if (skill.effects == null) return;
    foreach (var bleed in actor.statuses.Where(s => CombatEffectRules.Equals(s.status, "Bleed")).ToArray())
      if (actor.hp > 0) DealPure(null, actor, (int)MathF.Round(bleed.potency * Math.Max(1, bleed.stacks) * bleed.damageMultiplier), "Bleed");
    bool attacked = false;
    var targetSets = new Dictionary<string, List<CombatUnit>>();
    foreach (var eff in skill.effects) {
      string rule = OptionalOr(eff.target, skill.target);
      if (!targetSets.TryGetValue(rule, out var targets)) targetSets.Add(rule, targets = SelectTargets(rule, actor, allies, enemies));
      bool single = rule == "EnemyFront" || rule == "EnemyBack" || rule == "LowestHpEnemy";
      foreach (var t in targets) {
        if (actor.hp <= 0) return;
        attacked |= eff.type == "Damage";
        var recipient = eff.type == "Damage" && single ? Redirect(actor, t) : t;
        ApplyEffect(actor, recipient, allies, enemies, eff);
      }
    }
    if (attacked) RemoveStatus(actor, "Stealth");
  }

  private List<CombatUnit> SelectTargets(string targetRule, CombatUnit actor, List<CombatUnit> allies, List<CombatUnit> enemies) {
    enemies = enemies.Where(u => u.hp > 0 && !HasStatus(u, "Intangible")).OrderBy(u => u.formationSlot).ToList();
    allies  = allies.Where(u => u.hp > 0).OrderBy(u => u.formationSlot).ToList();
    if (targetRule == "EnemyFront" || targetRule == "EnemyBack" || targetRule == "LowestHpEnemy") {
      enemies = enemies.Where(u => !HasStatus(u, "Stealth") || HasStatus(actor, "Vantage") ||
        (actor.role == "Nuker" && Bonus(actor).Pair("Nuker", "Scout") && HasStatus(u, "Marked"))).ToList();
      var taunt = enemies.FirstOrDefault(u => HasStatus(u, "Taunt"));
      if (taunt != null) return new() { taunt };
    }

    return targetRule switch {
      "Self" => new List<CombatUnit> { actor },
      "AllEnemies" => enemies,
      "AllAllies" => allies,
      "LowestHpAlly" => allies.OrderBy(u => u.hp).Take(1).ToList(),
      "LowestHpEnemy" => enemies.OrderBy(u => u.hp).Take(1).ToList(),
      "SelfAndLowestHpAlly" => new[] { actor }.Concat(allies.Where(u => u != actor).OrderBy(u => u.hp).Take(1)).ToList(),
      "AllyFrontRow" => FormationRules.LivingRow(allies, true, true),
      "AdjacentAllies" => allies.Where(u => u != actor && FormationRules.Adjacent(actor.formationSlot, u.formationSlot)).ToList(),
      "Attacker" => triggerOther == null ? new() : new() { triggerOther },
      "EnemyFront" => FormationRules.LivingRow(enemies, true, true).Take(1).ToList(),
      "EnemyBack" => FormationRules.LivingRow(enemies, false, true).Take(1).ToList(),
      "EnemyFrontRow" => FormationRules.LivingRow(enemies, true, true),
      "EnemyBackRow" => FormationRules.LivingRow(enemies, false, true),
      "AllyRow" => FormationRules.LivingSameRow(allies, actor.formationSlot),
      "RandomEnemy2" => enemies.OrderBy(_ => _rng.Next()).Take(2).ToList(),
      _ => throw new InvalidOperationException("Unsupported target: " + targetRule)
    };
  }

  private void ApplyEffect(CombatUnit actor, CombatUnit target, List<CombatUnit> allies, List<CombatUnit> enemies, EffectDef eff) {
    if (target.hp <= 0) return;
    if (HasStatus(target, "Intangible")) { Emit(BattleEventKind.EffectBlocked, actor, target, 0, "Intangible"); return; }

    float roll = (float)_rng.NextDouble();
    if (eff.type == "ApplyStatus" && roll >= EffectiveStatusChance(actor, target, eff)) return;

    switch (eff.type) {
      case "Damage": {
        if (Dodged(actor, target, eff)) { redirectedHit.Remove(target); break; }
        int dmg = CalcEffectDamage(actor, target, eff);
        var result = DealDamage(actor, target, dmg, skillContext, eff.ignoreShield);
        if (result.totalDamage > 0) {
          GainEnergy(target, TakeDamageEnergyGain);
          GainEnergy(actor, result.killed ? KillEnergyGain : DealDamageEnergyGain);
          float reflected = target.statuses.Where(x => string.Equals(x.status, "Thorns", StringComparison.OrdinalIgnoreCase)).Sum(x => x.potency);
          // Reflection is damage, not another hit; it cannot recursively reflect.
          if (reflected > 0) DealPure(target, actor, (int)MathF.Round(result.totalDamage * reflected), "Thorns");
          float reflect = CombatEffectRules.Amount(target, "Reflect");
          if (reflect > 0) { RemoveStatus(target, "Reflect"); DealPure(target, actor, (int)MathF.Round(result.totalDamage * reflect), "Reflect"); }
          HitBonuses(actor, target, result.totalDamage);
          if (usingBasic) Trigger(actor, "BasicHit", target);
          Trigger(actor, "DamageDealt", target);
          Trigger(target, "DamageTaken", actor);
        }
        break;
      }
      case "Heal": {
        int amt = ScaledAmount(actor, target, eff, "POT");
        Heal(actor, target, amt);
        break;
      }
      case "Shield": {
        int amt = ScaledAmount(actor, target, eff, "TargetMaxHP");
        target.shield += (int)MathF.Round(amt * (1 + actor.shieldBonus));
        Emit(BattleEventKind.Shield, actor, target, (int)MathF.Round(amt * (1 + actor.shieldBonus)), skillContext);
        if (amt > 0) GrantSupportEnergy(actor);
        break;
      }
      case "ApplyStatus": {
        if (ApplyStatusFrom(actor, target, eff)) GrantSupportEnergy(actor);
        break;
      }
      default: ApplyUtility(actor, target, eff); break;
    }
  }

  private float EffectiveStatusChance(CombatUnit actor, CombatUnit target, EffectDef eff) {
    float baseChance = eff.chance;
    if (baseChance <= 0f) return 0f;
    if (actor.side == target.side) return Clamp01(baseChance);
    float potencyBonus = Stat(actor, "POT") / 1000f;
    float resistance = Stat(target, "POT") / 2000f * (Bonus(actor).Class("Tactician") >= 4 ? .8f : 1);
    return Clamp01(baseChance + potencyBonus - resistance);
  }

  private void ApplyStatus(CombatUnit source, CombatUnit target, EffectDef eff) {
    string name = eff.status ?? string.Empty;
    if (string.IsNullOrWhiteSpace(name)) return;

    bool stackable = IsStackableStatus(name);
    var existing = target.statuses.FirstOrDefault(s => string.Equals(s.status, name, StringComparison.OrdinalIgnoreCase));

    if (existing == null || stackable) {
      // Each application owns its potency and duration; stacks add linearly.
      target.statuses.Add(new StatusInstance {
        status = name, remainingTurns = eff.duration == -1 ? -1 : Math.Max(1, eff.duration),
        potency = eff.potency, stacks = 1, sourceId = source.instanceId, sourceRole = source.role,
        damageMultiplier = 1 + Bonus(source).DotBonus,
      });
      Emit(BattleEventKind.StatusApplied, source, target, eff.duration == -1 ? -1 : Math.Max(1, eff.duration), name);
      return;
    }

    existing.remainingTurns = existing.remainingTurns == -1 || eff.duration == -1 ? -1 : Math.Max(existing.remainingTurns, eff.duration);
    existing.potency = Math.Max(existing.potency, eff.potency);
    existing.sourceId = source.instanceId; existing.sourceRole = source.role;
    existing.damageMultiplier = 1 + Bonus(source).DotBonus;
    Emit(BattleEventKind.StatusRefreshed, source, target, existing.remainingTurns, name);
  }

  private static bool IsStackableStatus(string status) {
    string key = (status ?? string.Empty).Trim().ToLowerInvariant();
    return key == "poison" || key == "burn" || key == "bleed" || key == "regen" || key == "immortality" || key == "cloak" || key == "freeze";
  }

  private bool HasStatus(CombatUnit unit, string status) {
    return unit.statuses.Any(s => string.Equals(s.status, status, StringComparison.OrdinalIgnoreCase));
  }

  private (int totalDamage, bool killed) DealDamage(CombatUnit source, CombatUnit t, int dmg, string reason, bool ignoreShield = false) {
    if (t.hp <= 0 || HasStatus(t, "Intangible")) return (0, false);
    dmg = (int)MathF.Round(dmg * Math.Max(0, 1 + CombatEffectRules.Amount(t, "Vulnerability")) *
      Math.Max(0, 1 - CombatEffectRules.Amount(t, "DamageReduction") - Bonus(t).Reduction(t)));
    if (redirectedHit.Remove(t)) {
      dmg = (int)MathF.Round(dmg * Math.Max(0, 1 - CombatEffectRules.Amount(t, "RedirectReduction")));
      if (Bonus(t).Pair("Cover", "Survivor")) {
        dmg = (int)MathF.Round(dmg * .8f);
      }
    }
    bool wasAlive = t.hp > 0;
    int before = t.hp + t.shield;
    if (t.shield > 0 && !ignoreShield) {
      int absorbed = Math.Min(t.shield, dmg);
      t.shield -= absorbed;
      dmg -= absorbed;
    }
    if (dmg > 0) t.hp = Math.Max(0, t.hp - dmg);
    if (t.hp == 0 && HasStatus(t, "Immortality")) { Consume(t, "Immortality"); t.hp = 1; }
    if (t.shield == 0) RemoveStatus(t, "FrostShield");

    int after = t.hp + t.shield;
    int dealt = Math.Max(0, before - after);
    Emit(BattleEventKind.Damage, source, t, dealt, reason);
    if (wasAlive && t.hp <= 0) Emit(BattleEventKind.UnitDied, source, t, 0, reason);
    return (dealt, t.hp <= 0);
  }

  private void DealPure(CombatUnit source, CombatUnit t, int dmg, string reason) {
    _ = DealDamage(source, t, Math.Max(0, dmg), reason);
  }

  private void Heal(CombatUnit source, CombatUnit t, int amt) {
    if (t.hp <= 0 || HasStatus(t, "HealBlock") || HasStatus(t, "Intangible")) return;
    int before = t.hp;
    t.hp = Math.Min(t.maxHp, t.hp + Math.Max(0, (int)MathF.Round(amt * (1 + Bonus(t).HealingBonus + (source?.healingBonus ?? 0f)))));
    Emit(BattleEventKind.Heal, source ?? actingUnit, t, t.hp - before, skillContext ?? "Regen");
    if (t.hp > before) {
      GrantSupportEnergy(source ?? actingUnit);
      if (t.role == "Brawler" && Bonus(t).Pair("Brawler", "Medic")) ApplyStatusFrom(actingUnit ?? t, t,
        new EffectDef { status = "ATKUp", potency = .15f, duration = 1 });
    }
  }

  private void GrantSupportEnergy(CombatUnit source) {
    if (source != null && source.hp > 0 && Bonus(source).Class("Support") >= 4) GainEnergy(source, 5);
  }

  private void GainEnergy(CombatUnit unit, int amount) {
    int before = unit.energy;
    unit.energy = Math.Max(0, unit.energy + Math.Max(0, amount));
    if (unit.energy != before) Emit(BattleEventKind.EnergyChanged, actingUnit, unit, unit.energy - before, skillContext ?? "turn-start");
  }

  private void SpendEnergy(CombatUnit unit, int amount) {
    int before = unit.energy;
    unit.energy = Math.Max(0, unit.energy - Math.Max(0, amount));
    if (unit.energy != before) Emit(BattleEventKind.EnergyChanged, unit, unit, unit.energy - before, skillContext);
  }

  private void Emit(BattleEventKind kind, CombatUnit actor, CombatUnit target, int amount = 0, string detail = null) =>
    Trace?.Invoke(kind, actor, target, amount, detail);

  private static float Clamp01(float value) {
    if (value < 0f) return 0f;
    if (value > 1f) return 1f;
    return value;
  }
}
