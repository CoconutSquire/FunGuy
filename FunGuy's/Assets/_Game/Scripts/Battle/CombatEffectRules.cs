using System;
using System.Collections.Generic;
using System.Linq;

// battle-actions-v2 content vocabulary; unknown rules fail content validation.
public static class CombatEffectRules
{
    public static readonly HashSet<string> Targets = new(StringComparer.Ordinal) {
        "Self", "AllEnemies", "AllAllies", "EnemyFront", "EnemyBack", "EnemyFrontRow", "EnemyBackRow",
        "AllyRow", "RandomEnemy2", "LowestHpAlly", "LowestHpEnemy", "SelfAndLowestHpAlly", "AllyFrontRow",
        "AdjacentAllies", "Attacker"
    };
    public static readonly HashSet<string> Effects = new(StringComparer.Ordinal) {
        "Damage", "Heal", "Shield", "ApplyStatus", "Cleanse", "Dispel", "Energy", "Cooldown", "Gauge", "Move"
    };
    public static readonly HashSet<string> Debuffs = new(StringComparer.OrdinalIgnoreCase) {
        "Poison", "Burn", "Bleed", "Freeze", "Stun", "Silence", "Root", "Charm", "Confusion", "Vulnerability",
        "Brittle", "ATKDown", "DEFDown", "POTDown", "Slow", "HealBlock", "Marked"
    };
    public static readonly HashSet<string> Buffs = new(StringComparer.OrdinalIgnoreCase) {
        "Regen", "Thorns", "Reflect", "Stealth", "Intangible", "Vantage", "Luminescence", "FrostShield",
        "Immortality", "Cloak", "ATKUp", "DEFUp", "POTUp", "SPDUp", "DamageReduction", "Immunity", "Taunt", "Cover", "RedirectReduction"
    };
    public static readonly HashSet<string> Triggers = new(StringComparer.Ordinal) {
        "BattleStart", "TurnStart", "BasicHit", "DamageDealt", "DamageTaken", "EnergyGranted"
    };
    public static bool IsStatus(string name) => name != null && (Buffs.Contains(name) || Debuffs.Contains(name) || KeywordRules.IsCsvBackedStatus(name));
    public static bool IsKnownStatus(string name) => IsStatus(name) || InternalStatuses.Contains(name ?? string.Empty);
    private static readonly HashSet<string> InternalStatuses = new(StringComparer.OrdinalIgnoreCase) {
        "Thorns", "Taunt", "Cover", "DamageReduction", "RedirectReduction", "Marked", "Slow", "ATKUp", "ATKDown",
        "DEFUp", "DEFDown", "POTUp", "POTDown", "SPDUp", "SPDDown", "HealBlock", "Immunity"
    };
    public static bool IsDot(string name) => Equals(name, "Poison") || Equals(name, "Burn") || Equals(name, "Bleed");
    public static bool Equals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    public static bool Has(CombatUnit u, string name) => u.statuses.Any(s => Equals(s.status, name));
    public static float Amount(CombatUnit u, string name)
    {
        float amount = u.statuses.Where(s => Equals(s.status, name)).Sum(s => s.potency * Math.Max(1, s.stacks));
        if (amount > 0f) return amount;
        if (Equals(name, "Vulnerability") && Has(u, "Vulnerability")) return KeywordRules.VulnerabilityDamageTakenBonus;
        return amount;
    }
    public static EffectDef Clone(EffectDef e) => new() { type = e.type, scale = e.scale, status = e.status,
        chance = e.chance, duration = e.duration, potency = e.potency, target = e.target, stat = e.stat,
        ignoreDefense = e.ignoreDefense, ignoreShield = e.ignoreShield, sureHit = e.sureHit, slot = e.slot };
    public static List<PassiveDef> ClonePassives(List<PassiveDef> source) => source?.Select(p => new PassiveDef {
        id = p.id, trigger = p.trigger, target = p.target, every = p.every,
        effects = p.effects.Select(Clone).ToList() }).ToList() ?? new();
}
