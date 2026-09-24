using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleSim
{
    private List<CombatUnit> all;
    private readonly Dictionary<TeamSide, FormationBonuses> bonuses = new();
    private readonly HashSet<CombatUnit> coverUsed = new();
    private int passiveDepth;
    private CombatUnit triggerOther;
    private bool usingBasic;
    private float grantedFraction;
    private readonly HashSet<CombatUnit> redirectedHit = new();
    private FormationBonuses Bonus(CombatUnit u) => bonuses[u.side];
    private int Stat(CombatUnit u, string stat) => Bonus(u).Stat(u, stat);
    private static string OptionalOr(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
    internal IReadOnlyList<string> GetFormationBonuses(TeamSide side) => bonuses[side].Active;

    internal void Initialize(List<CombatUnit> player, List<CombatUnit> enemy)
    {
        all = player.Concat(enemy).ToList();
        bonuses[TeamSide.Player] = new FormationBonuses(player);
        bonuses[TeamSide.Enemy] = new FormationBonuses(enemy);
        foreach (var side in bonuses)
            foreach (string label in side.Value.Active) Emit(BattleEventKind.SynergyActivated, null, null, (int)side.Key, label);
        var fresh = all.Where(u => !u.encounterStarted && u.hp > 0).ToHashSet();
        foreach (var u in fresh.OrderBy(u => u.side).ThenBy(u => u.formationSlot)) {
            u.encounterStarted = true;
            int extra = (int)MathF.Round(u.maxHp * Bonus(u).Tier("Forest"));
            u.maxHp += extra; u.hp += extra;
            if (extra > 0) Emit(BattleEventKind.SynergyActivated, u, u, extra, "Forest HP");
        }
        foreach (var u in fresh.OrderBy(u => u.side).ThenBy(u => u.formationSlot)) Trigger(u, "BattleStart", u);
        foreach (var team in new[] { player, enemy }) {
            var wall = team.Where(u => u.hp > 0 && u.role == "Wall").OrderBy(u => u.formationSlot).FirstOrDefault();
            if (wall != null && Bonus(wall).Pair("Wall", "Cover") && fresh.Contains(wall)) {
                var ally = team.Where(u => u.hp > 0).OrderBy(u => u.hp).ThenBy(u => u.formationSlot).First();
                AddShield(wall, ally, (int)MathF.Round(wall.maxHp * .2f), "Fortress");
            }
        }
    }

    private void TurnBonuses(CombatUnit actor)
    {
        if (Bonus(actor).Biome("Forest") >= 5) Heal(actor, (int)MathF.Round(actor.maxHp * .05f));
        // Each front fighter's third owner turn, avoiding speed-dependent team-wide trigger spam.
        if (Bonus(actor).Class("Tank") >= 4 && FormationRules.Depth(actor.formationSlot) == 0 && actor.turnsTaken % 3 == 0)
            AddShield(actor, actor, (int)MathF.Round(actor.maxHp * .15f), "Unbreakable");
    }

    private void Trigger(CombatUnit owner, string trigger, CombatUnit other)
    {
        if (owner.hp <= 0 || passiveDepth > 0) return;
        var previousActor = actingUnit; var previousContext = skillContext; var previousOther = triggerOther;
        passiveDepth++;
        try {
            actingUnit = owner; triggerOther = other;
            foreach (var passive in owner.passives ?? new()) {
                if (passive.trigger != trigger || (trigger == "TurnStart" && owner.turnsTaken % Math.Max(1, passive.every) != 0)) continue;
                skillContext = passive.id;
                Emit(BattleEventKind.PassiveTriggered, owner, owner, 0, passive.id);
                var allies = all.Where(u => u.side == owner.side).ToList();
                var enemies = all.Where(u => u.side != owner.side).ToList();
                foreach (var effect in passive.effects)
                    foreach (var target in SelectTargets(OptionalOr(effect.target, passive.target), owner, allies, enemies))
                        if (owner.hp > 0) ApplyEffect(owner, target, allies, enemies, effect);
            }
        } finally { passiveDepth--; actingUnit = previousActor; skillContext = previousContext; triggerOther = previousOther; }
    }

    private CombatUnit Redirect(CombatUnit attacker, CombatUnit target)
    {
        var guard = all.Where(u => u.side == target.side && u != target && u.hp > 0 && HasStatus(u, "Cover") &&
            !HasStatus(u, "Intangible") && !coverUsed.Contains(u) &&
            (CombatEffectRules.Amount(u, "Cover") >= 1 || FormationRules.Adjacent(u.formationSlot, target.formationSlot)))
            .OrderBy(u => u.formationSlot).FirstOrDefault();
        if (guard == null) return target;
        coverUsed.Add(guard);
        redirectedHit.Add(guard);
        Emit(BattleEventKind.Redirected, target, guard, 0, "Cover");
        return guard;
    }

    private bool Dodged(CombatUnit actor, CombatUnit target, EffectDef effect)
    {
        if (effect.sureHit || HasStatus(actor, "Vantage") || HasStatus(target, "Root") ||
            (actor.role == "Nuker" && Bonus(actor).Pair("Nuker", "Scout") && HasStatus(target, "Marked"))) return false;
        if (HasStatus(target, "Cloak")) {
            Consume(target, "Cloak"); Emit(BattleEventKind.Miss, actor, target, 0, "Cloak"); return true;
        }
        if (Bonus(target).Class("Assassin") >= 4 && target.turnsTaken < 2 && _rng.NextDouble() < .2) {
            Emit(BattleEventKind.Miss, actor, target, 0, "Shadow Step"); return true;
        }
        return false;
    }

    private int CalcEffectDamage(CombatUnit actor, CombatUnit target, EffectDef effect)
    {
        float ignore = effect.ignoreDefense;
        if (HasStatus(actor, "Stealth") && Bonus(actor).Pair("Stealth", "Stalker")) ignore = 1;
        int defense = (int)MathF.Round(Stat(target, "DEF") * (1 - Math.Clamp(ignore, 0, 1)));
        int damage = DamageCalculator.Calculate(Stat(actor, OptionalOr(effect.stat, "ATK")), defense, (decimal)effect.scale,
            BiomeRules.Parse(actor.biome), BiomeRules.Parse(target.biome));
        float multiplier = 1 + Bonus(actor).DamageBonus(actor, target);
        float crit = (Bonus(actor).Class("DPS") >= 2 ? .1f : 0) + CombatEffectRules.Amount(actor, "Luminescence") +
            (HasStatus(target, "Brittle") ? .25f : 0);
        if (crit > 0 && _rng.NextDouble() < Math.Clamp(crit, 0, 1)) {
            multiplier *= 1.5f + (Bonus(actor).Class("Assassin") >= 2 ? .2f : 0);
            Emit(BattleEventKind.Critical, actor, target, 0, skillContext);
        }
        return (int)MathF.Round(damage * multiplier);
    }

    private void HitBonuses(CombatUnit actor, CombatUnit target, int damage)
    {
        if (target.role == "Cover" && Bonus(target).Pair("Cover", "Survivor"))
            foreach (var survivor in all.Where(u => u.side == target.side && u.hp > 0 && u.role == "Survivor")) GainEnergy(survivor, 10);
        if (Bonus(actor).Biome("Decay") >= 5 && actor.hp > 0) Heal(actor, (int)MathF.Round(damage * .1f));
        if (Bonus(actor).Biome("Tundra") >= 5 && target.hp > 0 && _rng.NextDouble() < .1)
            ApplyStatusFrom(actor, target, new EffectDef { status = "Freeze", duration = 1 });
        if (actor.hp > 0 && target.role == "Taunt" && Bonus(target).Pair("Taunt", "DoT"))
            ApplyStatusFrom(target, actor, new EffectDef { status = "Poison", potency = .02f, duration = 2 });
    }

    private int ScaledAmount(CombatUnit actor, CombatUnit target, EffectDef effect, string fallback)
    {
        string stat = OptionalOr(effect.stat, fallback);
        float value = stat == "MaxHP" ? actor.maxHp : stat == "TargetMaxHP" ? target.maxHp : Stat(actor, stat);
        if (stat == "GrantedEnergy") value = actor.maxHp * grantedFraction;
        return (int)MathF.Round(value * effect.scale);
    }

    private void AddShield(CombatUnit actor, CombatUnit target, int amount, string detail)
    {
        target.shield = checked(target.shield + amount);
        Emit(BattleEventKind.Shield, actor, target, amount, detail);
        if (amount > 0) GrantSupportEnergy(actor);
    }

    private void ApplyStatusFrom(CombatUnit source, CombatUnit target, EffectDef effect)
    {
        bool debuff = CombatEffectRules.Debuffs.Contains(effect.status);
        if (target.hp <= 0 || HasStatus(target, "Intangible") || (debuff && HasStatus(target, "Immunity")) ||
            (CombatEffectRules.Equals(effect.status, "Freeze") && target.shield > 0 && HasStatus(target, "FrostShield"))) {
            Emit(BattleEventKind.EffectBlocked, source, target, 0, effect.status); return false;
        }
        var applied = CombatEffectRules.Clone(effect);
        if (applied.duration != -1 && Bonus(source).Biome("Kitchen") >= 2 && (!debuff || CombatEffectRules.IsDot(applied.status))) applied.duration++;
        if (source.role == "Captain" && target.role == "Berserker" && !debuff && Bonus(source).Pair("Captain", "Berserker")) applied.potency *= 1.25f;
        if (target.statuses.Count(s => CombatEffectRules.Equals(s.status, applied.status)) >= 20) {
            Emit(BattleEventKind.EffectBlocked, source, target, 0, "stack-cap:" + applied.status); return false;
        }
        ApplyStatus(source, target, applied);
        if (!debuff && source.side == target.side) {
            if (source.role == "Buffer" && Bonus(source).Pair("Buffer", "Battery")) GainEnergy(target, 10);
            if (source.role == "Captain" && Bonus(source).Pair("Captain", "Battery")) GainEnergy(target, 5);
        }
        return true;
    }

    private void Consume(CombatUnit unit, string name)
    {
        var status = unit.statuses.First(s => CombatEffectRules.Equals(s.status, name));
        if (status.stacks > 1) { status.stacks--; Emit(BattleEventKind.StatusTicked, unit, unit, status.stacks, name); }
        else { unit.statuses.Remove(status); Emit(BattleEventKind.StatusExpired, unit, unit, 0, name); }
    }

    private void RemoveStatus(CombatUnit unit, string name)
    {
        foreach (var s in unit.statuses.Where(s => CombatEffectRules.Equals(s.status, name)).ToArray()) {
            unit.statuses.Remove(s); Emit(BattleEventKind.StatusExpired, actingUnit, unit, 0, name);
        }
    }

    private void ApplyUtility(CombatUnit actor, CombatUnit target, EffectDef effect)
    {
        switch (effect.type) {
            case "Energy": {
                int before = target.energy;
                GainEnergy(target, (int)MathF.Round(effect.potency));
                float previous = grantedFraction;
                grantedFraction = (target.energy - before) / (float)Math.Max(1, target.maxEnergy);
                if (target.energy > before) Trigger(actor, "EnergyGranted", target);
                grantedFraction = previous;
                break;
            }
            case "Cooldown":
                target.ultCdRemaining = Math.Max(0, target.ultCdRemaining - (int)MathF.Round(effect.potency));
                Emit(BattleEventKind.CooldownChanged, actor, target, target.ultCdRemaining, skillContext); break;
            case "Gauge":
                target.actionGauge = Math.Clamp(target.actionGauge + effect.potency, 0, 2000);
                Emit(BattleEventKind.GaugeChanged, actor, target, (int)effect.potency, skillContext); break;
            case "Cleanse": case "Dispel": {
                bool cleanse = effect.type == "Cleanse";
                var removable = target.statuses.Where(s => cleanse ? CombatEffectRules.Debuffs.Contains(s.status) : CombatEffectRules.Buffs.Contains(s.status)).ToArray();
                int limit = effect.potency <= 0 ? removable.Length : (int)effect.potency;
                int removed = 0;
                foreach (var status in removable.Take(limit)) {
                    target.statuses.Remove(status); removed++; Emit(BattleEventKind.StatusExpired, actor, target, 0, effect.type + ":" + status.status);
                }
                if (removed > 0) GrantSupportEnergy(actor);
                if (cleanse && removable.Length > 0 && actor.role == "Purifier" && Bonus(actor).Pair("Purifier", "Cover"))
                    foreach (var cover in all.Where(u => u.side == actor.side && u.hp > 0 && u.role == "Cover"))
                        AddShield(actor, cover, (int)MathF.Round(cover.maxHp * .1f), "Purifying Ward");
                break;
            }
            case "Move": {
                if (HasStatus(target, "Root")) { Emit(BattleEventKind.EffectBlocked, actor, target, 0, "Root"); break; }
                var occupied = all.Where(u => u.side == target.side && u.hp > 0).Select(u => u.formationSlot).ToHashSet();
                var free = Enumerable.Range(0, FormationRules.SlotCount).Where(s => !occupied.Contains(s)).ToArray();
                int slot = effect.slot < 0 ? (free.Length == 0 ? -1 : free[_rng.Next(free.Length)]) : effect.slot;
                if (slot < 0 || slot >= FormationRules.SlotCount || occupied.Contains(slot)) {
                    Emit(BattleEventKind.EffectBlocked, actor, target, 0, "occupied-or-invalid-cell"); break;
                }
                target.formationSlot = slot; Emit(BattleEventKind.Moved, actor, target, slot, skillContext); break;
            }
            default: throw new InvalidOperationException("Unsupported effect: " + effect.type);
        }
    }
}
