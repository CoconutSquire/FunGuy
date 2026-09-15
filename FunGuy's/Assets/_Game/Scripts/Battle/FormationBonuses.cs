using System;
using System.Collections.Generic;
using System.Linq;

// Deployed biome/class counts are locked for the encounter. Role pairs need living partners.
public sealed class FormationBonuses
{
    private readonly List<CombatUnit> team;
    public FormationBonuses(List<CombatUnit> team) { this.team = team; }
    public int Biome(string name) => team.Count(u => CombatEffectRules.Equals(u.biome, name));
    public int Class(string name) => team.Count(u => CombatEffectRules.Equals(u.classArchetype, name));
    public bool Pair(string a, string b) => team.Any(u => u.hp > 0 && u.role == a) && team.Any(u => u.hp > 0 && u.role == b);
    public float Tier(string biome) => Biome(biome) >= 3 ? .1f : Biome(biome) >= 2 ? .05f : 0;
    public float DotBonus => (Class("Mage") >= 2 ? .15f : 0) + (Biome("Wetlands") >= 5 ? .1f : 0);
    public float HealingBonus => (Class("Support") >= 2 ? .15f : 0) + (Pair("Wall", "Medic") ? .2f : 0);
    public int Stat(CombatUnit u, string stat)
    {
        float value = stat switch { "ATK" => u.atk, "DEF" => u.def, "SPD" => u.spd, _ => u.pot };
        float bonus = stat switch {
            "ATK" => Tier("Decay") + CombatEffectRules.Amount(u, "ATKUp") - CombatEffectRules.Amount(u, "ATKDown"),
            "DEF" => Tier("Tundra") + (Class("Tank") >= 2 ? .1f : 0) + (Pair("Wall", "Medic") ? .05f : 0) +
                CombatEffectRules.Amount(u, "DEFUp") - CombatEffectRules.Amount(u, "DEFDown") - (CombatEffectRules.Has(u, "Burn") ? .1f : 0),
            "SPD" => CombatEffectRules.Amount(u, "SPDUp") - CombatEffectRules.Amount(u, "Slow"),
            _ => Tier("Wetlands") + CombatEffectRules.Amount(u, "POTUp") - CombatEffectRules.Amount(u, "POTDown")
        };
        return Math.Max(stat == "SPD" ? 1 : 0, (int)Math.Round(value * Math.Max(0, 1 + bonus)) + (stat == "SPD" && Class("Tactician") >= 2 ? 10 : 0));
    }
    public float DamageBonus(CombatUnit actor, CombatUnit target) =>
        (Class("DPS") >= 4 && target.hp < target.maxHp * .4f ? .2f : 0) +
        (actor.role == "Duelist" && Pair("CC", "Duelist") && new[] { "Freeze", "Stun", "Burn", "Taunt", "Root" }.Any(s => CombatEffectRules.Has(target, s)) ? .25f : 0) +
        (actor.role == "AoE" && Pair("Saboteur", "AoE") && target.statuses.Any(s => s.sourceRole == "Saboteur" && CombatEffectRules.Debuffs.Contains(s.status)) ? .2f : 0);
    public float Reduction(CombatUnit target) => target.role == "Grunt" && Pair("Grunt", "Wall") &&
        team.Any(u => u.hp > u.maxHp * .5f && u.role == "Wall") ? .3f : 0;
    private static readonly (string a, string b, string name)[] Pairs = {
        ("Wall", "Medic", "Vanguard"), ("Nuker", "Scout", "Sniper Nest"), ("CC", "Duelist", "Shatter"),
        ("Taunt", "DoT", "Attrition"), ("Captain", "Battery", "The Battery"), ("Captain", "Berserker", "Gourmet Line"),
        ("Wall", "Cover", "Fortress"), ("Brawler", "Medic", "Brawling Pair"), ("Buffer", "Battery", "Overclock"),
        ("Grunt", "Wall", "Phalanx"), ("Purifier", "Cover", "Purifying Ward"), ("Stealth", "Stalker", "Shadow Step"),
        ("Cover", "Survivor", "Not Alone"), ("Saboteur", "AoE", "Cataclysm")
    };
    public IReadOnlyList<string> Active => Array.AsReadOnly(new[] { "Forest", "Wetlands", "Decay", "Tundra", "Kitchen" }
        .Where(b => Biome(b) >= 2).Select(b => $"biome:{b}:{(Biome(b) >= 5 ? 5 : Biome(b) >= 3 && b != "Kitchen" ? 3 : 2)}")
        .Concat(new[] { "Tank", "DPS", "Mage", "Support", "Tactician", "Assassin" }
        .Where(c => Class(c) >= 2).Select(c => $"class:{c}:{(Class(c) >= 4 ? 4 : 2)}"))
        .Concat(Pairs.Where(p => Pair(p.a, p.b)).Select(p => "role:" + p.name)).ToArray());
}
